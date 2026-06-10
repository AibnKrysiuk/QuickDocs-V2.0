using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using QuickDocs.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace QuickDocs.UI.ViewModels
{
    public partial class RemitoViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrlRemitos = "http://localhost:5018/api/remitos";
        private const string ApiUrlClientes = "http://localhost:5018/api/clientes";
        private const string ApiUrlItems = "http://localhost:5018/api/items";

        // Id del remito en caso de que estemos MODIFICANDO uno existente
        private int _remitoIdActual = 0;

        // ID del presupuesto si este remito se generó a partir de uno
        private int? _presupuestoIdOrigen;

        // --- Listas de ayuda para autocompletar desde la API ---
        private List<Cliente> _todosLosClientes = new();
        private List<Item> _todosLosItems = new();

        // --- Colecciones de texto plano para los desplegables de la UI ---
        public ObservableCollection<string> SugerenciasClientes { get; } = new();
        public ObservableCollection<string> SugerenciasItems { get; } = new();

        public IRelayCommand NavegarAHistorialCommand { get; }

        // --- Bindings de la Cabecera ---
        [ObservableProperty]
        private string _textoBuscarCliente = string.Empty;

        [ObservableProperty]
        private Cliente? _clienteSeleccionado;

        // 🎯 CAMBIO LOGÍSTICO: Dirección de entrega obligatoria para el remito
        [ObservableProperty]
        private string _direccionEntrega = string.Empty;

        // --- Bindings del formulario de ingreso de Renglones ---
        [ObservableProperty]
        private string _textoBuscarItem = string.Empty;

        [ObservableProperty]
        private Item? _itemSeleccionado;

        [ObservableProperty]
        private string _descripcionRenglon = string.Empty;

        [ObservableProperty]
        private string _marcaRenglon = string.Empty;

        [ObservableProperty]
        private decimal _cantidadRenglon = 1;

        [ObservableProperty]
        private decimal _precioRenglon = 0;

        // --- Totales Finales (Para control interno del formulario) ---
        [ObservableProperty]
        private decimal _total = 0;

        // --- Colección de Renglones de la Grilla Actual (Reutiliza el mismo molde temporal) ---
        public ObservableCollection<DetallePresupuestoTemporal> Detalles { get; } = new();

        // --- Estado de Selección de la Grilla Actual ---
        [ObservableProperty]
        private DetallePresupuestoTemporal? _detalleSeleccionado;

        // --- Comandos ---
        public IAsyncRelayCommand CargarDatosInicialesCommand { get; }
        public IRelayCommand AgregarRenglonCommand { get; }
        public IRelayCommand QuitarRenglonCommand { get; }
        public IRelayCommand SeleccionarRenglonParaModificarCommand { get; }
        public IAsyncRelayCommand GuardarRemitoCommand { get; }

        public RemitoViewModel()
        {
            _httpClient = new HttpClient();

            CargarDatosInicialesCommand = new AsyncRelayCommand(CargarDatosInicialesAsync);
            AgregarRenglonCommand = new RelayCommand(AgregarRenglon);
            QuitarRenglonCommand = new RelayCommand(QuitarRenglon);
            SeleccionarRenglonParaModificarCommand = new RelayCommand(SeleccionarRenglonParaModificar);
            GuardarRemitoCommand = new AsyncRelayCommand(GuardarRemitoAsync);
            NavegarAHistorialCommand = new RelayCommand(NavegarAHistorial);

            // Carga asíncrona de clientes e ítems al iniciar
            Dispatcher.UIThread.Post(async () => await CargarDatosInicialesAsync());
        }

        private async Task CargarDatosInicialesAsync()
        {
            try
            {
                var clientes = await _httpClient.GetFromJsonAsync<List<Cliente>>($"{ApiUrlClientes}?usuarioId=1");
                _todosLosClientes = clientes ?? new List<Cliente>();

                var items = await _httpClient.GetFromJsonAsync<List<Item>>($"{ApiUrlItems}?usuarioId=1");
                _todosLosItems = items ?? new List<Item>();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SugerenciasClientes.Clear();
                    foreach (var name in _todosLosClientes.Select(c => c.Nombre).Where(n => !string.IsNullOrEmpty(n)))
                    {
                        SugerenciasClientes.Add(name);
                    }

                    SugerenciasItems.Clear();
                    foreach (var desc in _todosLosItems.Select(i => i.Descripcion).Where(d => !string.IsNullOrEmpty(d)))
                    {
                        SugerenciasItems.Add(desc);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al precargar catálogos en Remitos: {ex.Message}");
            }
        }

        partial void OnItemSeleccionadoChanged(Item? value)
        {
            if (value == null) return;
            DescripcionRenglon = value.Descripcion;
            MarcaRenglon = value.Marca ?? "Sin Marca";
            PrecioRenglon = value.PrecioUnitario;
            CantidadRenglon = 1;
        }

        private void AgregarRenglon()
        {
            if (string.IsNullOrWhiteSpace(DescripcionRenglon) || CantidadRenglon <= 0) return;

            if (DetalleSeleccionado != null)
            {
                Detalles.Remove(DetalleSeleccionado);
                DetalleSeleccionado = null;
            }

            var nuevoRenglon = new DetallePresupuestoTemporal
            {
                ItemId = ItemSeleccionado?.Id,
                Descripcion = DescripcionRenglon,
                Marca = MarcaRenglon,
                Cantidad = CantidadRenglon,
                PrecioUnitario = PrecioRenglon
            };

            Detalles.Add(nuevoRenglon);
            RecalcularTotal();
            LimpiarCamposRenglon();
        }

        private void QuitarRenglon()
        {
            if (DetalleSeleccionado == null) return;
            Detalles.Remove(DetalleSeleccionado);
            RecalcularTotal();
            DetalleSeleccionado = null;
        }

        private void SeleccionarRenglonParaModificar()
        {
            if (DetalleSeleccionado == null) return;

            DescripcionRenglon = DetalleSeleccionado.Descripcion;
            MarcaRenglon = DetalleSeleccionado.Marca;
            CantidadRenglon = DetalleSeleccionado.Cantidad;
            PrecioRenglon = DetalleSeleccionado.PrecioUnitario;
            
            ItemSeleccionado = _todosLosItems.FirstOrDefault(i => i.Id == DetalleSeleccionado.ItemId);
        }

        private void RecalcularTotal()
        {
            Total = Detalles.Sum(d => d.Importe);
        }

        private async Task GuardarRemitoAsync()
        {
            if (Detalles.Count == 0) return;

            // Armamos el DTO adaptado de manera idéntica a presupuestos
            var dto = new
            {
                UsuarioId = 1,
                ClienteId = ClienteSeleccionado?.Id, 
                ClienteNombreLibre = ClienteSeleccionado == null ? TextoBuscarCliente : null,
                DireccionEntrega = DireccionEntrega,
                PresupuestoId = _presupuestoIdOrigen ?? 0,
                DescuentoGeneral = 0.0, 
                Detalles = Detalles.Select(d => new
                {
                    ItemId = d.ItemId ?? 0,
                    Descripcion = d.Descripcion, 
                    Cantidad = d.Cantidad
                }).ToList()
            };

            try
            {
                HttpResponseMessage response;

                // 🎯 LÓGICA DE CONVERSIÓN/EDICIÓN: Si ya existe un ID, hacemos PUT para actualizar
                if (_remitoIdActual > 0)
                {
                    Console.WriteLine($"[DEBUG] Modificando Remito existente ID: {_remitoIdActual} mediante PUT...");
                    response = await _httpClient.PutAsJsonAsync($"http://localhost:5018/api/remitos/{_remitoIdActual}", dto);
                }
                else
                {
                    Console.WriteLine("[DEBUG] Creando nuevo Remito mediante POST...");
                    response = await _httpClient.PostAsJsonAsync("http://localhost:5018/api/remitos", dto);
                }

                if (!response.IsSuccessStatusCode)
                {
                    string errorApi = await response.Content.ReadAsStringAsync();
                    throw new Exception($"La API de Remitos devolvió un error ({response.StatusCode}): {errorApi}");
                }

                string jsonRespuesta = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"[DEBUG] Respuesta exitosa de la API (Remito): {jsonRespuesta}");

                // 🎯 Extracción 100% segura del ID usando System.Text.Json (Evita mezclar con IDs de los detalles)
                int idGenerado = 0;
                try
                {
                    using (var doc = System.Text.Json.JsonDocument.Parse(jsonRespuesta))
                    {
                        if (doc.RootElement.TryGetProperty("id", out var idProp))
                        {
                            idGenerado = idProp.GetInt32();
                        }
                    }
                }
                catch (Exception exJson)
                {
                    Console.WriteLine($"[WARN] Error al parsear JSON con JsonDocument: {exJson.Message}");
                    // Fallback por Regex estricta si algo falla
                    var matchCierre = System.Text.RegularExpressions.Regex.Match(jsonRespuesta, @"""id""\s*:\s*(\d+)\s*,\s*""usuarioId""");
                    if (matchCierre.Success) idGenerado = int.Parse(matchCierre.Groups[1].Value);
                }

                if (idGenerado > 0)
                {
                    Console.WriteLine($"[DEBUG] Intentando descargar e imprimir PDF para Remito ID: {idGenerado}");
                    await DescargarYAbrirPdfAsync(idGenerado);
                }
                else
                {
                    Console.WriteLine("[WARN] No se pudo determinar el ID del remito guardado.");
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LimpiarFormularioCompleto();
                });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"==================================================");
                System.Console.WriteLine($"🚨 ERROR CRÍTICO EN GUARDAR REMITO:");
                System.Console.WriteLine(ex.ToString());
                System.Console.WriteLine($"==================================================");
            }
        }

        private async Task DescargarYAbrirPdfAsync(int remitoId)
        {
            try
            {
                string urlPdf = $"{ApiUrlRemitos}/{remitoId}/pdf";
                Console.WriteLine($"[DEBUG] Pidiendo bytes a la URL: {urlPdf}");
                
                byte[] pdfBytes = await _httpClient.GetByteArrayAsync(urlPdf);
                Console.WriteLine($"[DEBUG] Bytes recibidos con éxito. Tamaño: {pdfBytes.Length} bytes.");

                string carpetaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string carpetaQuickDocs = System.IO.Path.Combine(carpetaDocumentos, "QuickDocs", "Remitos");

                if (!System.IO.Directory.Exists(carpetaQuickDocs))
                {
                    System.IO.Directory.CreateDirectory(carpetaQuickDocs);
                }

                string rutaArchivo = System.IO.Path.Combine(carpetaQuickDocs, $"Remito_{remitoId}.pdf");
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
                System.Console.WriteLine($"🚨 ERROR CRÍTICO EN DESCARGA/APERTURA PDF REMITO:");
                System.Console.WriteLine(ex.ToString());
                System.Console.WriteLine($"==================================================");
            }
        }

        // Método auxiliar para cuando cargamos un Remito existente desde el historial

        public async Task CargarRemitoExistente(Documento documentoBase)
        {
            // 1. Usamos el ID del documento base para hacer una petición GET específica
            // Esto garantiza que el Backend traiga el objeto con todos sus detalles incluidos
            try 
            {
                string url = $"http://localhost:5018/api/remitos/{documentoBase.Id}";
                var remito = await _httpClient.GetFromJsonAsync<Remito>(url);

                if (remito == null)
                {
                    System.Console.WriteLine("[ERROR] No se pudo obtener el remito desde la API.");
                    return;
                }

                // 2. Ahora cargamos los datos con el objeto 'remito' ya completo y bien tipado
                _remitoIdActual = remito.Id;
                _presupuestoIdOrigen = remito.PresupuestoId;

                // Espera a que los catálogos estén listos
                int intentos = 0;
                while ((_todosLosClientes.Count == 0 || _todosLosItems.Count == 0) && intentos < 30)
                {
                    await Task.Delay(100); 
                    intentos++;
                }

                ClienteSeleccionado = _todosLosClientes.FirstOrDefault(c => c.Id == remito.ClienteId);

                if (ClienteSeleccionado != null)
                {
                    TextoBuscarCliente = ClienteSeleccionado.Nombre ?? string.Empty;
                }
                else
                {
                    TextoBuscarCliente = remito.ClienteNombre ?? string.Empty;
                }

                DireccionEntrega = remito.DireccionEntrega ?? string.Empty;

                Detalles.Clear();
                if (remito.Detalles != null)
                {
                    foreach (var det in remito.Detalles)
                    {
                        var itemDelCatalogo = _todosLosItems.FirstOrDefault(i => i.Id == det.ItemId);
                        Detalles.Add(new DetallePresupuestoTemporal
                        {
                            ItemId = det.ItemId,
                            Descripcion = det.DescripcionSnapshot,
                            Marca = itemDelCatalogo?.Marca ?? "Sin Marca", 
                            Cantidad = det.Cantidad,
                            PrecioUnitario = det.PrecioAplicado
                        });
                    }
                }
                
                RecalcularTotal();
                System.Console.WriteLine($"[DEBUG] Remito {remito.Id} cargado exitosamente mediante API.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ERROR] Falló la carga del remito: {ex.Message}");
            }
        }

        // 🎯 MÉTODO DE CONVERSIÓN: Para cuando abrís el remito trayendo los bultos de un Presupuesto
        public void CargarDesdePresupuestoDeOrigen(Presupuesto presupuesto)
        {
            LimpiarFormularioCompleto();
            
            _presupuestoIdOrigen = presupuesto.Id;
            ClienteSeleccionado = _todosLosClientes.FirstOrDefault(c => c.Id == presupuesto.ClienteId);
            TextoBuscarCliente = ClienteSeleccionado != null ? (ClienteSeleccionado.Nombre ?? string.Empty) : (presupuesto.ClienteNombre ?? string.Empty);

            if (presupuesto.Detalles != null)
            {
                foreach (var det in presupuesto.Detalles)
                {
                    var itemDelCatalogo = _todosLosItems.FirstOrDefault(i => i.Id == det.ItemId);
                    Detalles.Add(new DetallePresupuestoTemporal
                    {
                        ItemId = det.ItemId,
                        Descripcion = det.DescripcionSnapshot,
                        Marca = itemDelCatalogo?.Marca ?? "Sin Marca",
                        Cantidad = det.Cantidad,
                        PrecioUnitario = det.PrecioAplicado
                    });
                }
            }
            RecalcularTotal();
        }

        partial void OnTextoBuscarClienteChanged(string value)
        {
            var coincidencia = _todosLosClientes.FirstOrDefault(c => string.Equals(c.Nombre, value, StringComparison.OrdinalIgnoreCase));
            if (coincidencia != null)
            {
                ClienteSeleccionado = coincidencia;
            }
            else
            {
                ClienteSeleccionado = null; 
            }
        }

        partial void OnTextoBuscarItemChanged(string value)
        {
            DescripcionRenglon = value; 
            var coincidencia = _todosLosItems.FirstOrDefault(i => string.Equals(i.Descripcion, value, StringComparison.OrdinalIgnoreCase));
            if (coincidencia != null)
            {
                ItemSeleccionado = coincidencia;
                MarcaRenglon = coincidencia.Marca ?? "Sin Marca";
                PrecioRenglon = coincidencia.PrecioUnitario;
            }
            else
            {
                ItemSeleccionado = null; 
            }
        }

        private void LimpiarCamposRenglon()
        {
            ItemSeleccionado = null;
            DescripcionRenglon = string.Empty;
            MarcaRenglon = string.Empty;
            CantidadRenglon = 1;
            PrecioRenglon = 0;
        }

        private void LimpiarFormularioCompleto()
        {
            _remitoIdActual = 0;
            _presupuestoIdOrigen = null;
            ClienteSeleccionado = null;
            TextoBuscarCliente = string.Empty;
            DireccionEntrega = string.Empty;
            Detalles.Clear();
            Total = 0;
            LimpiarCamposRenglon();
        }
    
        private void NavegarAHistorial()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainDataContext = desktop.MainWindow?.DataContext;
                if (mainDataContext != null)
                {
                    var propiedadComando = mainDataContext.GetType().GetProperty("MostrarHistorial");
                    if (propiedadComando != null)
                    {
                        var comando = propiedadComando.GetValue(mainDataContext) as System.Windows.Input.ICommand;
                        if (comando != null && comando.CanExecute(null))
                        {
                            comando.Execute(null);
                        }
                    }
                }
            }
        }
    }
}