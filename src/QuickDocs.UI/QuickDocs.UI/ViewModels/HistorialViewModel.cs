using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using QuickDocs.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace QuickDocs.UI.ViewModels
{
    public partial class HistorialViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrlPresupuestos = "http://localhost:5018/api/presupuestos";

        // Colección reactiva para la lista de la pantalla
        public ObservableCollection<Presupuesto> Documentos { get; } = new();

        [ObservableProperty]
        private string _textoBuscar = string.Empty;

        [ObservableProperty]
        private string _labelCantidadResultados = "Cargando documentos...";

        // Comandos
        public IAsyncRelayCommand CargarHistorialCommand { get; }
        public IAsyncRelayCommand<Presupuesto> ReimprimirPdfCommand { get; }
        public IAsyncRelayCommand<Presupuesto> BorrarPresupuestoCommand { get; }
        public IRelayCommand<Presupuesto> EditarPresupuestoCommand { get; }

        // Delegado/Evento para avisarle a la ventana principal que queremos saltar a la pestaña de edición
        public Action<Presupuesto>? OnSolicitarModificacion { get; set; }

        public HistorialViewModel()
        {
            _httpClient = new HttpClient();

            CargarHistorialCommand = new AsyncRelayCommand(CargarHistorialAsync);
            ReimprimirPdfCommand = new AsyncRelayCommand<Presupuesto>(ReimprimirPdfAsync);
            BorrarPresupuestoCommand = new AsyncRelayCommand<Presupuesto>(BorrarPresupuestoAsync);
            EditarPresupuestoCommand = new RelayCommand<Presupuesto>(EditarPresupuesto);

            // Carga inicial automática al instanciarse
            Dispatcher.UIThread.Post(async () => await CargarHistorialAsync());
        }

        public async Task CargarHistorialAsync()
        {
            // 🕵️‍♂️ LOG AL EMPEZAR
            System.Console.WriteLine($"[FRONT-DIAG] ¡Disparando CargarHistorialAsync! Solicitando a: {ApiUrlPresupuestos}?usuarioId=1");

            try
            {
                var lista = await _httpClient.GetFromJsonAsync<List<Presupuesto>>($"{ApiUrlPresupuestos}?usuarioId=1");
                
                // 🕵️‍♂️ LOG DE RECEPCIÓN
                System.Console.WriteLine($"[FRONT-DIAG] Respuesta recibida de la API. Elementos devueltos: {(lista != null ? lista.Count : "NULL")}");

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Documentos.Clear();
                    if (lista != null)
                    {
                        foreach (var p in lista)
                        {
                            Documentos.Add(p);
                        }
                    }
                    LabelCantidadResultados = $"Mostrando {Documentos.Count} documentos encontrados.";
                });
            }
            catch (Exception ex)
            {
                // 🕵️‍♂️ LOG EN CASO DE ERROR REAL
                System.Console.WriteLine($"==================================================");
                System.Console.WriteLine($"🚨 [FRONT-DIAG] FALLÓ LA PETICIÓN AL HISTORIAL:");
                System.Console.WriteLine(ex.ToString());
                System.Console.WriteLine($"==================================================");
                
                LabelCantidadResultados = "Error al conectar con el servidor.";
            }
        }

        private async Task ReimprimirPdfAsync(Presupuesto? presupuesto)
        {
            if (presupuesto == null) return;

            try
            {
                string urlPdf = $"{ApiUrlPresupuestos}/{presupuesto.Id}/pdf";
                byte[] pdfBytes = await _httpClient.GetByteArrayAsync(urlPdf);

                string carpetaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string carpetaQuickDocs = System.IO.Path.Combine(carpetaDocumentos, "QuickDocs", "Presupuestos");

                if (!System.IO.Directory.Exists(carpetaQuickDocs))
                    System.IO.Directory.CreateDirectory(carpetaQuickDocs);

                string rutaArchivo = System.IO.Path.Combine(carpetaQuickDocs, $"Presupuesto_{presupuesto.Id}.pdf");
                await System.IO.File.WriteAllBytesAsync(rutaArchivo, pdfBytes);

                // Apertura nativa en Linux con xdg-open
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = $"\"{rutaArchivo}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error al reimprimir PDF: {ex.Message}");
            }
        }

        private async Task BorrarPresupuestoAsync(Presupuesto? presupuesto)
        {
            if (presupuesto == null) return;

            try
            {
                // Pegamos al nuevo endpoint DELETE que creamos recién en el backend
                var response = await _httpClient.DeleteAsync($"{ApiUrlPresupuestos}/{presupuesto.Id}");
                if (response.IsSuccessStatusCode)
                {
                    // Si el backend borró con éxito, limpiamos de la lista visual sin recargar todo
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Documentos.Remove(presupuesto);
                        LabelCantidadResultados = $"Mostrando {Documentos.Count} documentos encontrados.";
                    });
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error al borrar presupuesto: {ex.Message}");
            }
        }

        // Este método se ejecuta al clickear "Editar" en la grilla
        private void EditarPresupuesto(Presupuesto? presupuesto)
        {
            if (presupuesto == null) return;

            System.Console.WriteLine($"[DEBUG] ¡Click en Editar! Intentando procesar ID: {presupuesto.Id}");
            OnSolicitarModificacion?.Invoke(presupuesto);
        }
    }
}