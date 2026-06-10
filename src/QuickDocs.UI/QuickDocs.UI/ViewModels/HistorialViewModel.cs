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
        
        // 🎯 Endpoint base unificado para traer el historial general de documentos
        private const string ApiUrlDocumentos = "http://localhost:5018/api/documentos";

        // 🎯 Colección polimórfica que soporta cualquier tipo derivado de Documento
        public ObservableCollection<Documento> Documentos { get; } = new();

        [ObservableProperty]
        private string _textoBuscar = string.Empty;

        // 🎯 Enlazado al SelectedIndex del ComboBox de la vista (0 = Todos, 1 = Presupuestos, 2 = Remitos)
        [ObservableProperty]
        private int _tipoDocumentoSeleccionado = 0;

        [ObservableProperty]
        private string _labelCantidadResultados = "Cargando documentos...";

        [ObservableProperty]
        private int _periodoSeleccionado = 0;

        // Comandos unificados usando la clase base Documento
        public IAsyncRelayCommand CargarHistorialCommand { get; }
        public IAsyncRelayCommand<Documento> ReimprimirPdfCommand { get; }
        public IAsyncRelayCommand<Documento> BorrarDocumentoCommand { get; }
        public IRelayCommand<Documento> EditarDocumentoCommand { get; }

        // Delegado modificado para notificar la edición de cualquier documento
        public Action<Documento>? OnSolicitarModificacion { get; set; }

        public HistorialViewModel()
        {
            _httpClient = new HttpClient();

            CargarHistorialCommand = new AsyncRelayCommand(CargarHistorialAsync);
            ReimprimirPdfCommand = new AsyncRelayCommand<Documento>(ReimprimirPdfAsync);
            BorrarDocumentoCommand = new AsyncRelayCommand<Documento>(BorrarDocumentoAsync);
            EditarDocumentoCommand = new RelayCommand<Documento>(EditarDocumento);

            // Carga inicial automática al instanciarse
            Dispatcher.UIThread.Post(async () => await CargarHistorialAsync());
        }

        public async Task CargarHistorialAsync()
        {
            // 🎯 Construimos la URL con los parámetros correspondientes para el endpoint unificado
            // tipoFiltro: 0 = Todos, 1 = Presupuestos, 2 = Remitos
            string urlQuery = $"{ApiUrlDocumentos}?usuarioId=1&tipoFiltro={TipoDocumentoSeleccionado}&buscarCliente={Uri.EscapeDataString(TextoBuscar)}&periodoFiltro={PeriodoSeleccionado}";
            

            System.Console.WriteLine($"[FRONT-DIAG] ¡Disparando CargarHistorialAsync! Solicitando a: {urlQuery}");

            try
            {
                var lista = await _httpClient.GetFromJsonAsync<List<Documento>>(urlQuery);
                
                System.Console.WriteLine($"[FRONT-DIAG] Respuesta recibida de la API. Elementos devueltos: {(lista != null ? lista.Count : "NULL")}");

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Documentos.Clear();
                    if (lista != null)
                    {
                        foreach (var d in lista)
                        {
                            Documentos.Add(d);
                        }
                    }
                    LabelCantidadResultados = $"Mostrando {Documentos.Count} documentos encontrados.";
                });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"==================================================");
                System.Console.WriteLine($"🚨 [FRONT-DIAG] FALLÓ LA PETICIÓN AL HISTORIAL:");
                System.Console.WriteLine(ex.ToString());
                System.Console.WriteLine($"==================================================");
                
                LabelCantidadResultados = "Error al conectar con el servidor.";
            }
        }

        private async Task ReimprimirPdfAsync(Documento? documento)
        {
            if (documento == null) return;

            try
            {
                // 🎯 Determinamos la subcarpeta, el prefijo y el controlador según el tipo real del documento
                string subcarpeta;
                string prefijo;
                string urlControllerBase;

                switch (documento.Tipo)
                {
                    case TipoDocumento.Remito:
                        subcarpeta = "Remitos";
                        prefijo = "Remito";
                        urlControllerBase = "remitos";
                        break;

                    case TipoDocumento.Recibo:
                        subcarpeta = "Recibos";
                        prefijo = "Recibo";
                        urlControllerBase = "recibos";
                        break;
                    
                    case TipoDocumento.NotaCredito:
                        subcarpeta = "NotasCredito";
                        prefijo = "NotaCredito";
                        urlControllerBase = "notascredito";
                        break;

                    case TipoDocumento.Presupuesto:
                    default:
                        subcarpeta = "Presupuestos";
                        prefijo = "Presupuesto";
                        urlControllerBase = "presupuestos";
                        break;
                }
                
                string urlPdf = $"http://localhost:5018/api/{urlControllerBase}/{documento.Id}/pdf";
                System.Console.WriteLine($"[FRONT-DIAG] Reimprimiendo {documento.Tipo}. Solicitando PDF a: {urlPdf}");

                byte[] pdfBytes = await _httpClient.GetByteArrayAsync(urlPdf);

                string carpetaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string carpetaQuickDocs = System.IO.Path.Combine(carpetaDocumentos, "QuickDocs", subcarpeta);

                if (!System.IO.Directory.Exists(carpetaQuickDocs))
                    System.IO.Directory.CreateDirectory(carpetaQuickDocs);

                string rutaArchivo = System.IO.Path.Combine(carpetaQuickDocs, $"{prefijo}_{documento.Id}.pdf");
                await System.IO.File.WriteAllBytesAsync(rutaArchivo, pdfBytes);

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
                else
                {
                    // Aprovechamos a dejarle el soporte para Windows también por si acaso
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c start \"\" \"{rutaArchivo}\"",
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

        private async Task BorrarDocumentoAsync(Documento? documento)
        {
            if (documento == null) return;

            try
            {
                // 🎯 Determinamos el controlador correcto de la API según el tipo de documento
                string urlControllerBase = documento.Tipo switch
                {
                    TipoDocumento.Remito => "remitos",
                    TipoDocumento.Recibo => "recibos",
                    TipoDocumento.NotaCredito => "notascredito",
                    TipoDocumento.Presupuesto => "presupuestos",
                    _ => "presupuestos"
                };

                var response = await _httpClient.DeleteAsync($"http://localhost:5018/api/{urlControllerBase}/{documento.Id}");
                
                if (response.IsSuccessStatusCode)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Documentos.Remove(documento);
                        LabelCantidadResultados = $"Mostrando {Documentos.Count} documentos encontrados.";
                    });
                }
                else
                {
                    System.Console.WriteLine($"[ERROR] No se pudo borrar el documento. Código: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error al borrar documento: {ex.Message}");
            }
        }



        private void EditarDocumento(Documento? documento)
        {
            if (documento == null) return;

            System.Console.WriteLine($"[DEBUG] ¡Click en Editar! Intentando procesar ID: {documento.Id}, Tipo: {documento.Tipo}");
            OnSolicitarModificacion?.Invoke(documento);
        }

        

        // Se dispara automáticamente al cambiar el período en el ComboBox
        partial void OnPeriodoSeleccionadoChanged(int value)
        {
            _ = CargarHistorialAsync();
        }

        // 🎯 Se ejecuta automáticamente cada vez que cambia el texto de búsqueda
        partial void OnTextoBuscarChanged(string value)
        {
            // Dispara la consulta en segundo plano sin congelar la UI
            _ = CargarHistorialAsync();
        }

        // 🎯 Se ejecuta automáticamente cada vez que se selecciona un tipo diferente en el ComboBox
        partial void OnTipoDocumentoSeleccionadoChanged(int value)
        {
            _ = CargarHistorialAsync();
        }

        [RelayCommand]
        public void LimpiarFiltros()
        {
            TextoBuscar = string.Empty;
            TipoDocumentoSeleccionado = 0;
            PeriodoSeleccionado = 0;
            
            // NOTA: No hace falta llamar a CargarHistorialAsync() acá, 
            // porque al resetear las propiedades, los métodos "On...Changed" 
            // se van a disparar solos y van a refrescar la lista automáticamente.
        }
    }
}