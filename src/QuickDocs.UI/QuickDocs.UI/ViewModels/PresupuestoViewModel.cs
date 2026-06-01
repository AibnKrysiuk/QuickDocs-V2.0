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

        // --- Bindings de la Cabecera ---
        [ObservableProperty]
        private string _textoBuscarCliente = string.Empty;

        [ObservableProperty]
        private Cliente? _clienteSeleccionado;

        [ObservableProperty]
        private int _diasValidez = 15;

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

        public PresupuestoViewModel()
        {
            _httpClient = new HttpClient();

            CargarDatosInicialesCommand = new AsyncRelayCommand(CargarDatosInicialesAsync);
            AgregarRenglonCommand = new RelayCommand(AgregarRenglon);
            QuitarRenglonCommand = new RelayCommand(QuitarRenglon);
            SeleccionarRenglonParaModificarCommand = new RelayCommand(SeleccionarRenglonParaModificar);
            GuardarPresupuestoCommand = new AsyncRelayCommand(GuardarPresupuestoAsync);

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al precargar catálogos: {ex.Message}");
            }
        }

        // Interceptamos la selección de un ítem para autocompletar el bloque de ingreso
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

            // Si el ítem ya estaba seleccionado en la grilla para modificarse, lo quitamos para actualizarlo
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
            
            // Buscamos si corresponde a un ítem existente en el catálogo
            ItemSeleccionado = _todosLosItems.FirstOrDefault(i => i.Id == DetalleSeleccionado.ItemId);
        }

        private void RecalcularTotal()
        {
            Total = Detalles.Sum(d => d.Importe);
        }

        private async Task GuardarPresupuestoAsync()
        {
            if (Detalles.Count == 0) return;

            // Armamos el DTO adaptado para soportar ítems libres del formulario
            var dto = new
            {
                UsuarioId = 1,
                ClienteId = ClienteSeleccionado?.Id ?? 0, 
                DescuentoGeneral = 0.0, 
                Detalles = Detalles.Select(d => new
                {
                    ItemId = d.ItemId ?? 0,
                    Descripcion = d.Descripcion, // Mandamos la descripción de la grilla
                    Precio = d.PrecioUnitario,   // Mandamos el precio de la grilla
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

                // Si la API respondió con error, forzamos a que salte al catch mostrando el motivo
                if (!response.IsSuccessStatusCode)
                {
                    string errorApi = await response.Content.ReadAsStringAsync();
                    throw new Exception($"La API devolvió un error ({response.StatusCode}): {errorApi}");
                }

                // Leemos la respuesta como string primero para evitar fallos de deserialización rígida
                string jsonRespuesta = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"[DEBUG] Respuesta exitosa de la API: {jsonRespuesta}");

                // Extraemos el ID usando manipulación de texto básica por si el objeto completo falla al parsear
                int idGenerado = 0;
                var oPresupuesto = await response.Content.ReadFromJsonAsync<Presupuesto>();
                if (oPresupuesto != null)
                {
                    idGenerado = oPresupuesto.Id;
                }
                else
                {
                    // Intento de rescate si el serializador falló por referencias circulares
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
                // Forzamos la salida por consola estándar para verlo en tu terminal de Pop!_OS
                System.Console.WriteLine($"==================================================");
                System.Console.WriteLine($"🚨 ERROR CRÍTICO EN GUARDAR PRESUPUESTO:");
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
        // 🔥 MÉTODO CLAVE PARA CUANDO VOLVAMOS DESDE EL HISTORIAL A MODIFICAR
        // 🔥 MODIFICADO: Ahora es "async Task" para esperar a la API si es necesario
        public async Task CargarPresupuestoExistente(Presupuesto presupuesto)
        {
            _presupuestoIdActual = presupuesto.Id;

            // ⏳ SALA DE ESPERA: Si los catálogos están vacíos, esperamos a que termine la carga inicial
            int intentos = 0;
            while ((_todosLosClientes.Count == 0 || _todosLosItems.Count == 0) && intentos < 30)
            {
                await Task.Delay(100); // Esperamos 100ms y volvemos a chequear
                intentos++;
            }

            // Asignamos el cliente buscando en el catálogo ya cargado
            ClienteSeleccionado = _todosLosClientes.FirstOrDefault(c => c.Id == presupuesto.ClienteId);
            
            if (presupuesto.FechaVencimiento >= presupuesto.FechaEmision)
            {
                DiasValidez = (presupuesto.FechaVencimiento - presupuesto.FechaEmision).Days;
            }

            // Rellenamos los renglones de la grilla
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
            System.Console.WriteLine($"[DEBUG-FORM] Éxito. Renglones cargados: {Detalles.Count}. Cliente: {ClienteSeleccionado?.Nombre}");
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
            DiasValidez = 15;
            Detalles.Clear();
            Total = 0;
            LimpiarCamposRenglon();
        }
    }

    // 📄 CLASE AUXILIAR LOCAL: Modela el estado dinámico de las celdas en la pantalla
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