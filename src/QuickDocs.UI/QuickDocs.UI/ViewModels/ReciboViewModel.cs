using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickDocs.Core.Models;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace QuickDocs.UI.ViewModels
{
    public partial class ReciboViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrlRecibos = "http://localhost:5018/api/recibos";
        private const string ApiUrlClientes = "http://localhost:5018/api/clientes";

        // Id del recibo en caso de que estemos MODIFICANDO uno existente
        private int _reciboIdActual = 0;

        private List<Cliente> _todosLosClientes = new();

        // --- Colecciones de texto plano para los desplegables de la UI ---
        public ObservableCollection<string> SugerenciasClientes { get; } = new();

        [ObservableProperty] 
        private decimal _importeRecibido;
        [ObservableProperty] 
        private MetodoPago _formaPago = MetodoPago.Efectivo;
        [ObservableProperty] 
        private string _detalle = string.Empty;

        [ObservableProperty]
        private string _textoBuscarCliente = string.Empty;

        [ObservableProperty]
        private Cliente? _clienteSeleccionado;

        // --- Comandos ---
        public IAsyncRelayCommand CargarDatosInicialesCommand { get; }
        public IAsyncRelayCommand GuardarReciboCommand { get; }
        public IRelayCommand NavegarAHistorialCommand { get; }

        public ReciboViewModel()
        {
            _httpClient = new HttpClient();

            CargarDatosInicialesCommand = new AsyncRelayCommand(CargarDatosInicialesAsync);
            GuardarReciboCommand = new AsyncRelayCommand(GuardarReciboAsync);
            
            Dispatcher.UIThread.Post(async () => await CargarDatosInicialesAsync());
        }

        private async Task CargarDatosInicialesAsync()
        {
            try
            {
                var clientes = await _httpClient.GetFromJsonAsync<List<Cliente>>($"{ApiUrlClientes}?usuarioId=1");
                _todosLosClientes = clientes ?? new List<Cliente>();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SugerenciasClientes.Clear();
                    foreach (var name in _todosLosClientes.Select(c => c.Nombre).Where(n => !string.IsNullOrEmpty(n)))
                    {
                        SugerenciasClientes.Add(name);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al precargar catálogos: {ex.Message}");
            }
        }

        // ========== Guardar/Crear un Recibo
        private async Task GuardarReciboAsync()
        {
            var dto = new
            {
                UsuarioId = 1,
                ClienteId = ClienteSeleccionado?.Id, 
                ClienteNombreLibre = ClienteSeleccionado == null ? TextoBuscarCliente : null,
                ImporteRecibido = ImporteRecibido,
                FormaPago = FormaPago,
                Detalle = Detalle
            };

            try
            {
                HttpResponseMessage response;
                if (_reciboIdActual == 0)
                {
                    response = await _httpClient.PostAsJsonAsync(ApiUrlRecibos, dto);
                }
                else
                {
                    response = await _httpClient.PutAsJsonAsync($"{ApiUrlRecibos}/{_reciboIdActual}", dto);
                }

                if (!response.IsSuccessStatusCode)
                {
                    string errorApi = await response.Content.ReadAsStringAsync();
                    throw new Exception($"La API devolvió un error ({response.StatusCode}): {errorApi}");
                }

                string jsonRespuesta = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"[DEBUG] Respuesta exitosa de la API: {jsonRespuesta}");

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
                    Console.WriteLine("[WARN] No se pudo determinar el ID del recibo guardado.");
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LimpiarFormularioCompleto();
                });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"==================================================");
                System.Console.WriteLine($"🚨 ERROR CRÍTICO EN GUARDAR RECIBO:");
                System.Console.WriteLine(ex.ToString());
                System.Console.WriteLine($"==================================================");
            }
        }

        private async Task DescargarYAbrirPdfAsync(int reciboId)
        {
            try
            {
                string urlPdf = $"{ApiUrlRecibos}/{reciboId}/pdf";
                Console.WriteLine($"[DEBUG] Pidiendo bytes a la URL: {urlPdf}");
                
                byte[] pdfBytes = await _httpClient.GetByteArrayAsync(urlPdf);
                Console.WriteLine($"[DEBUG] Bytes recibidos con éxito. Tamaño: {pdfBytes.Length} bytes.");

                string carpetaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string carpetaQuickDocs = System.IO.Path.Combine(carpetaDocumentos, "QuickDocs", "Presupuestos");

                if (!System.IO.Directory.Exists(carpetaQuickDocs))
                {
                    System.IO.Directory.CreateDirectory(carpetaQuickDocs);
                }

                string rutaArchivo = System.IO.Path.Combine(carpetaQuickDocs, $"Recibo_{reciboId}.pdf");
                await System.IO.File.WriteAllBytesAsync(rutaArchivo, pdfBytes);
                
                System.Console.WriteLine($"[OK] PDF guardado físicamente en: {rutaArchivo}");

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
            catch (Exception ex)
            {
                System.Console.WriteLine($"==================================================");
                System.Console.WriteLine($"🚨 ERROR CRÍTICO EN DESCARGA/APERTURA PDF:");
                System.Console.WriteLine(ex.ToString());
                System.Console.WriteLine($"==================================================");
            }
        }
        
        private void LimpiarFormularioCompleto()
        {
            _reciboIdActual = 0;
            ClienteSeleccionado = null;
            TextoBuscarCliente = string.Empty;
            ImporteRecibido = 0m;                  // 🎯 Restablecemos valores numéricos
            FormaPago = MetodoPago.Efectivo;       // 🎯 Restablecemos combo de pago
            Detalle = string.Empty;                // 🎯 Limpiamos campo detalle
        }

        // 🎯 IMPLEMENTADO: Carga el recibo de forma asíncrona mapeando los datos a la vista
        public async Task CargarReciboExistente(Documento documentoBase)
        {
            try
            {
                System.Console.WriteLine($"[DEBUG] ReciboViewModel -> Cargando recibo existente ID: {documentoBase.Id}");
                
                // Si por velocidad del hilo los clientes todavía no impactaron, forzamos la carga del catálogo
                if (_todosLosClientes == null || !_todosLosClientes.Any())
                {
                    await CargarDatosInicialesAsync();
                }

                var reciboReal = await _httpClient.GetFromJsonAsync<Recibo>($"{ApiUrlRecibos}/{documentoBase.Id}");
                if (reciboReal == null) throw new Exception("La API devolvió un objeto vacío para el Recibo.");

                // Seteamos la variable de estado de edición
                _reciboIdActual = reciboReal.Id;

                // Asignamos las propiedades observables del formulario
                ImporteRecibido = reciboReal.ImporteRecibido;
                FormaPago = reciboReal.FormaPago;
                Detalle = reciboReal.Detalle ?? string.Empty;

                // Resolución y vinculación del cliente en la interfaz
                if (reciboReal.ClienteId.HasValue && reciboReal.ClienteId.Value > 0)
                {
                    ClienteSeleccionado = _todosLosClientes.FirstOrDefault(c => c.Id == reciboReal.ClienteId.Value);
                    TextoBuscarCliente = ClienteSeleccionado?.Nombre ?? reciboReal.ClienteNombre ?? string.Empty;
                }
                else
                {
                    ClienteSeleccionado = null;
                    TextoBuscarCliente = reciboReal.ClienteNombre ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ERROR] Falló la carga del recibo en el ViewModel: {ex.Message}");
            }
        }
    }
}