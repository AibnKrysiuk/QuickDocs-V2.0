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
    public partial class PresupuestoViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrlPresupuestos = "http://localhost:5018/api/presupuestos";
        private const string ApiUrlClientes = "http://localhost:5018/api/clientes";
        private const string ApiUrlItems = "http://localhost:5018/api/items";

        // Id del presupuesto en caso de que estemos MODIFICANDO uno existente
        private int _presupuestoIdActual = 0;

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

        [ObservableProperty]
        private int _diasValidez = 15;

        // 🎯 PROPIEDADES NUEVAS: Para soportar CUIT y Dirección editables o del cliente seleccionado
        [ObservableProperty]
        private string _clienteCuitLibre = string.Empty;

        [ObservableProperty]
        private string _clienteDireccionLibre = string.Empty;

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

        // --- Totales Finales ---
        [ObservableProperty]
        private decimal _total = 0;

        // 🎯 NUEVO: Propiedad para controlar la visibilidad del botón "Convertir a Remito"
        [ObservableProperty]
        private bool _esEdicion = false;

        // --- Colección de Renglones de la Grilla Actual ---
        public ObservableCollection<DetallePresupuestoTemporal> Detalles { get; } = new();

        // --- Estado de Selección de la Grilla Actual (para modificar/quitar) ---
        [ObservableProperty]
        private DetallePresupuestoTemporal? _detalleSeleccionado;

        // --- Comandos ---
        public IAsyncRelayCommand CargarDatosInicialesCommand { get; }
        public IRelayCommand AgregarRenglonCommand { get; }
        public IRelayCommand QuitarRenglonCommand { get; }
        public IRelayCommand SeleccionarRenglonParaModificarCommand { get; }
        public IAsyncRelayCommand GuardarPresupuestoCommand { get; }
        
        // 🎯 NUEVO: Comando para ejecutar la conversión
        public IAsyncRelayCommand ConvertirARemitoCommand { get; }

        public PresupuestoViewModel()
        {
            _httpClient = new HttpClient();

            CargarDatosInicialesCommand = new AsyncRelayCommand(CargarDatosInicialesAsync);
            AgregarRenglonCommand = new RelayCommand(AgregarRenglon);
            QuitarRenglonCommand = new RelayCommand(QuitarRenglon);
            SeleccionarRenglonParaModificarCommand = new RelayCommand(SeleccionarRenglonParaModificar);
            GuardarPresupuestoCommand = new AsyncRelayCommand(GuardarPresupuestoAsync);
            ConvertirARemitoCommand = new AsyncRelayCommand(ConvertirARemitoAsync);
            NavegarAHistorialCommand = new RelayCommand(NavegarAHistorial);

            // Carga asíncrona de clientes e ítems para los selectores al iniciar
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
                System.Diagnostics.Debug.WriteLine($"Error al precargar catálogos: {ex.Message}");
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

        private async Task GuardarPresupuestoAsync()
        {
            if (Detalles.Count == 0) return;

            var dto = new
            {
                UsuarioId = 1,
                ClienteId = ClienteSeleccionado?.Id ?? 0, 
                ClienteNombreLibre = ClienteSeleccionado == null ? TextoBuscarCliente : null,
                ClienteCuitLibre = !string.IsNullOrWhiteSpace(this.ClienteCuitLibre) ? this.ClienteCuitLibre : null,
                ClienteDireccionLibre = !string.IsNullOrWhiteSpace(this.ClienteDireccionLibre) ? this.ClienteDireccionLibre : null,
                DiasValidez = DiasValidez,
                DescuentoGeneral = 0.0, 
                Detalles = Detalles.Select(d => new
                {
                    ItemId = d.ItemId ?? 0,
                    Descripcion = d.Descripcion, 
                    Precio = d.PrecioUnitario,   
                    Cantidad = d.Cantidad
                }).ToList()
            };

            try
            {
                HttpResponseMessage response;
                if (_presupuestoIdActual == 0)
                {
                    response = await _httpClient.PostAsJsonAsync(ApiUrlPresupuestos, dto);
                }
                else
                {
                    response = await _httpClient.PutAsJsonAsync($"{ApiUrlPresupuestos}/{_presupuestoIdActual}", dto);
                }

                if (!response.IsSuccessStatusCode)
                {
                    string errorApi = await response.Content.ReadAsStringAsync();
                    throw new Exception($"La API devolvió un error ({response.StatusCode}): {errorApi}");
                }

                string jsonRespuesta = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"[DEBUG] Respuesta exitosa de la API: {jsonRespuesta}");

                int idGenerado = 0;
                var oPresupuesto = await response.Content.ReadFromJsonAsync<Presupuesto>();
                if (oPresupuesto != null)
                {
                    idGenerado = oPresupuesto.Id;
                }
                else
                {
                    var match = System.Text.RegularExpressions.Regex.Match(jsonRespuesta, @"""id""\s*:\s*(\d+)");
                    if (match.Success) idGenerado = int.Parse(match.Groups[1].Value);
                }

                if (idGenerado > 0)
                {
                    Console.WriteLine($"[DEBUG] Intentando descargar e imprimir PDF para ID: {idGenerado}");
                    await DescargarYAbrirPdfAsync(idGenerado);
                }
                else
                {
                    Console.WriteLine("[WARN] No se pudo determinar el ID del presupuesto guardado.");
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LimpiarFormularioCompleto();
                });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"==================================================");
                System.Console.WriteLine($"🚨 ERROR CRÍTICO EN GUARDAR PRESUPUESTO:");
                System.Console.WriteLine(ex.ToString());
                System.Console.WriteLine($"==================================================");
            }
        }

        // 🎯 NUEVO: Lógica del comando de conversión a Remito
        private async Task ConvertirARemitoAsync()
        {
            if (_presupuestoIdActual == 0) return;

            try
            {
                string urlConvertir = $"{ApiUrlPresupuestos}/{_presupuestoIdActual}/convertir";
                var response = await _httpClient.PostAsync(urlConvertir, null);

                if (!response.IsSuccessStatusCode)
                {
                    string errorApi = await response.Content.ReadAsStringAsync();
                    throw new Exception($"La API devolvió un error al convertir ({response.StatusCode}): {errorApi}");
                }

                //Convertir a remito
                var remitoCreado = await response.Content.ReadFromJsonAsync<Remito>();
                if (remitoCreado != null)
                {
                    System.Console.WriteLine($"[OK] Presupuesto convertido a Remito con éxito. Nuevo ID: {remitoCreado.Id}");

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Disparamos la navegación directa al nuevo remito usando reflexión
                        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                        {
                            var mainDataContext = desktop.MainWindow?.DataContext;
                            if (mainDataContext != null)
                            {
                                var metodoNavegar = mainDataContext.GetType().GetMethod("NavegarADocumentoDirecto");
                                if (metodoNavegar != null)
                                {
                                    metodoNavegar.Invoke(mainDataContext, new object[] { remitoCreado });
                                }
                            }
                        }

                        LimpiarFormularioCompleto();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"==================================================");
                System.Console.WriteLine($"🚨 ERROR AL CONVERTIR A REMITO:");
                System.Console.WriteLine(ex.ToString());
                System.Console.WriteLine($"==================================================");
            }
        }

        private async Task DescargarYAbrirPdfAsync(int presupuestoId)
        {
            try
            {
                string urlPdf = $"{ApiUrlPresupuestos}/{presupuestoId}/pdf";
                Console.WriteLine($"[DEBUG] Pidiendo bytes a la URL: {urlPdf}");
                
                byte[] pdfBytes = await _httpClient.GetByteArrayAsync(urlPdf);
                Console.WriteLine($"[DEBUG] Bytes recibidos con éxito. Tamaño: {pdfBytes.Length} bytes.");

                string carpetaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string carpetaQuickDocs = System.IO.Path.Combine(carpetaDocumentos, "QuickDocs", "Presupuestos");

                if (!System.IO.Directory.Exists(carpetaQuickDocs))
                {
                    System.IO.Directory.CreateDirectory(carpetaQuickDocs);
                }

                string rutaArchivo = System.IO.Path.Combine(carpetaQuickDocs, $"Presupuesto_{presupuestoId}.pdf");
                await System.IO.File.WriteAllBytesAsync(rutaArchivo, pdfBytes);
                
                System.Console.WriteLine($"[OK] PDF guardado físicamente en: {rutaArchivo}");

                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    Console.WriteLine($"[DEBUG] Ejecutando xdg-open para el archivo...");
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

        public async Task CargarPresupuestoExistente(Documento documentoBase)
        {
            try 
            {
                string url = $"http://localhost:5018/api/presupuestos/{documentoBase.Id}";
                var presupuesto = await _httpClient.GetFromJsonAsync<Presupuesto>(url);

                if (presupuesto == null)
                {
                    System.Console.WriteLine("[ERROR] No se pudo obtener el presupuesto desde la API.");
                    return;
                }

                _presupuestoIdActual = presupuesto.Id;

                int intentos = 0;
                while ((_todosLosClientes.Count == 0 || _todosLosItems.Count == 0) && intentos < 30)
                {
                    await Task.Delay(100); 
                    intentos++;
                }

                ClienteSeleccionado = _todosLosClientes.FirstOrDefault(c => c.Id == presupuesto.ClienteId);

                if (ClienteSeleccionado != null)
                {
                    TextoBuscarCliente = ClienteSeleccionado.Nombre ?? string.Empty;
                    ClienteCuitLibre = ClienteSeleccionado.CuitCuil ?? string.Empty;
                    ClienteDireccionLibre = ClienteSeleccionado.Direccion ?? string.Empty;
                }
                else
                {
                    TextoBuscarCliente = presupuesto.ClienteNombre ?? string.Empty;
                    ClienteCuitLibre = string.Empty;
                    ClienteDireccionLibre = string.Empty;
                }

                if (presupuesto.FechaVencimiento >= presupuesto.FechaEmision)
                {
                    DiasValidez = (presupuesto.FechaVencimiento - presupuesto.FechaEmision).Days;
                }

                Detalles.Clear();
                if (presupuesto.Detalles != null)
                {
                    foreach (var det in presupuesto.Detalles)
                    {
                        Detalles.Add(new DetallePresupuestoTemporal
                        {
                            ItemId = det.ItemId,
                            Descripcion = det.DescripcionSnapshot,
                            Marca = "Sin Marca", 
                            Cantidad = det.Cantidad,
                            PrecioUnitario = det.PrecioAplicado
                        });
                    }
                }
                
                RecalcularTotal();
                
                // 🎯 NUEVO: Como estamos editando un registro que ya existe en la BD, activamos la bandera
                EsEdicion = true;

                System.Console.WriteLine($"[DEBUG-FORM] Éxito. Renglones cargados: {Detalles.Count}. Cliente: {presupuesto.ClienteNombre}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ERROR] Falló la carga del presupuesto: {ex.Message}");
            }
        }

        partial void OnTextoBuscarClienteChanged(string value)
        {
            var coincidencia = _todosLosClientes.FirstOrDefault(c => string.Equals(c.Nombre, value, StringComparison.OrdinalIgnoreCase));
            
            if (coincidencia != null)
            {
                ClienteSeleccionado = coincidencia;
                ClienteCuitLibre = coincidencia.CuitCuil ?? string.Empty;
                ClienteDireccionLibre = coincidencia.Direccion ?? string.Empty;
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
            _presupuestoIdActual = 0;
            ClienteSeleccionado = null;
            TextoBuscarCliente = string.Empty;
            ClienteCuitLibre = string.Empty;
            ClienteDireccionLibre = string.Empty;
            DiasValidez = 15;
            Detalles.Clear();
            Total = 0;
            
            // 🎯 NUEVO: Reseteamos el estado de edición al limpiar
            EsEdicion = false;
            
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

    public class DetallePresupuestoTemporal
    {
        public int? ItemId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Importe => Cantidad * PrecioUnitario;
    }

}