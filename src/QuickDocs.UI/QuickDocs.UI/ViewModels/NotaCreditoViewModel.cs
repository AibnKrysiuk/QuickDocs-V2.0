using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using QuickDocs.Core.Models;
using QuickDocs.Backend.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace QuickDocs.UI.ViewModels
{
    public partial class NotaCreditoViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiUrlNotas = "http://localhost:5018/api/notascredito";
        private const string ApiUrlClientes = "http://localhost:5018/api/clientes";

        private int _notaCreditoIdActual = 0;
        private List<Cliente> _todosLosClientes = new();

        public ObservableCollection<string> SugerenciasClientes { get; } = new();

        [ObservableProperty] private string _textoBuscarCliente = string.Empty;
        [ObservableProperty] private Cliente? _clienteSeleccionado;
        [ObservableProperty] private decimal _total;
        [ObservableProperty] private string _detalle = string.Empty;

        public IAsyncRelayCommand GuardarNotaCreditoCommand { get; }
        public IAsyncRelayCommand BorrarNotaCreditoCommand { get; }

        public NotaCreditoViewModel()
        {
            GuardarNotaCreditoCommand = new AsyncRelayCommand(GuardarNotaCreditoAsync);
            BorrarNotaCreditoCommand = new AsyncRelayCommand(BorrarNotaCreditoAsync);
            
            Dispatcher.UIThread.Post(async () => await CargarClientesAsync());
        }

        private async Task CargarClientesAsync()
        {
            try
            {
                var clientes = await _httpClient.GetFromJsonAsync<List<Cliente>>($"{ApiUrlClientes}?usuarioId=1");
                _todosLosClientes = clientes ?? new List<Cliente>();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SugerenciasClientes.Clear();
                    foreach (var name in _todosLosClientes.Select(c => c.Nombre).Where(n => !string.IsNullOrEmpty(n)))
                        SugerenciasClientes.Add(name);
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        partial void OnTextoBuscarClienteChanged(string value)
        {
            ClienteSeleccionado = _todosLosClientes.FirstOrDefault(c => 
                string.Equals(c.Nombre, value, StringComparison.OrdinalIgnoreCase));
        }

        private async Task GuardarNotaCreditoAsync()
        {
            var dto = new NotaCreditoCreateDto
            {
                UsuarioId = 1,
                ClienteId = ClienteSeleccionado?.Id,
                ClienteNombreLibre = ClienteSeleccionado == null ? TextoBuscarCliente : null,
                Total = Total,
                Detalle = Detalle
            };

            try
            {
                HttpResponseMessage response = _notaCreditoIdActual == 0 
                    ? await _httpClient.PostAsJsonAsync(ApiUrlNotas, dto)
                    : await _httpClient.PutAsJsonAsync($"{ApiUrlNotas}/{_notaCreditoIdActual}", dto);

                if (!response.IsSuccessStatusCode)
                {
                    string errorApi = await response.Content.ReadAsStringAsync();
                    throw new Exception($"La API devolvió un error ({response.StatusCode}): {errorApi}");
                }

                string jsonRespuesta = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"[DEBUG] Respuesta de la API: {jsonRespuesta}");

                int idGenerado = 0;
                
                try 
                {
                    using (var doc = System.Text.Json.JsonDocument.Parse(jsonRespuesta))
                    {
                        if (doc.RootElement.TryGetProperty("id", out var idProp) || 
                            doc.RootElement.TryGetProperty("Id", out idProp))
                        {
                            idGenerado = idProp.GetInt32();
                        }
                    }
                }
                catch (Exception jsonEx)
                {
                    System.Console.WriteLine($"[WARN] Falló el parseo directo del ID: {jsonEx.Message}");
                }

                if (idGenerado > 0)
                {
                    Console.WriteLine($"[DEBUG] Intentando descargar e imprimir PDF para ID: {idGenerado}");
                    await DescargarYAbrirPdfAsync(idGenerado);
                }
                else
                {
                    Console.WriteLine("[WARN] No se pudo determinar el ID de la Nota de Crédito.");
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LimpiarFormulario();
                });
            }
            catch (Exception ex) { System.Console.WriteLine($"[ERROR CRÍTICO] {ex.Message}"); }
        }

        private async Task DescargarYAbrirPdfAsync(int notaId)
        {
            try
            {
                string urlPdf = $"{ApiUrlNotas}/{notaId}/pdf";
                byte[] pdfBytes = await _httpClient.GetByteArrayAsync(urlPdf);

                string carpetaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string carpetaQuickDocs = System.IO.Path.Combine(carpetaDocumentos, "QuickDocs", "NotasCredito");

                if (!System.IO.Directory.Exists(carpetaQuickDocs))
                    System.IO.Directory.CreateDirectory(carpetaQuickDocs);

                string rutaArchivo = System.IO.Path.Combine(carpetaQuickDocs, $"NotaCredito_{notaId}.pdf");
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
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c start \"\" \"{rutaArchivo}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
            }
            catch (Exception ex) { System.Console.WriteLine($"[ERROR PDF] {ex.Message}"); }
        }

        private async Task BorrarNotaCreditoAsync()
        {
            if (_notaCreditoIdActual == 0) return;
            try
            {
                await _httpClient.DeleteAsync($"{ApiUrlNotas}/{_notaCreditoIdActual}");
                LimpiarFormulario();
            }
            catch (Exception ex) { System.Console.WriteLine($"[ERROR] {ex.Message}"); }
        }

        // 🎯 IMPLEMENTADO: Carga asíncrona con validación de catálogo para evitar desvinculaciones
        public async Task CargarNotaCreditoExistente(Documento documentoBase)
        {
            try
            {
                System.Console.WriteLine($"[DEBUG] NotaCreditoViewModel -> Cargando Nota de Crédito existente ID: {documentoBase.Id}");

                if (_todosLosClientes == null || !_todosLosClientes.Any())
                {
                    await CargarClientesAsync();
                }

                var nota = await _httpClient.GetFromJsonAsync<NotaCredito>($"{ApiUrlNotas}/{documentoBase.Id}");
                if (nota != null)
                {
                    _notaCreditoIdActual = nota.Id;
                    Total = nota.Total;
                    Detalle = nota.Detalle ?? string.Empty;

                    if (nota.ClienteId.HasValue && nota.ClienteId.Value > 0)
                    {
                        ClienteSeleccionado = _todosLosClientes.FirstOrDefault(c => c.Id == nota.ClienteId.Value);
                        TextoBuscarCliente = ClienteSeleccionado?.Nombre ?? nota.ClienteNombre ?? string.Empty;
                    }
                    else
                    {
                        ClienteSeleccionado = null;
                        TextoBuscarCliente = nota.ClienteNombre ?? string.Empty;
                    }
                }
            }
            catch (Exception ex) { System.Console.WriteLine($"[ERROR] Falló la carga de la Nota de Crédito: {ex.Message}"); }
        }

        private void LimpiarFormulario()
        {
            _notaCreditoIdActual = 0;
            TextoBuscarCliente = string.Empty;
            ClienteSeleccionado = null;
            Total = 0m;
            Detalle = string.Empty;
        }
    }
}