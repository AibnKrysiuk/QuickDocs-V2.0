using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickDocs.Core.Models;

namespace QuickDocs.UI.ViewModels
{
    public partial class InicioViewModel : ViewModelBase
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrlDocumentos = "http://localhost:5018/api/documentos";
        private const string ApiUrlClientes = "http://localhost:5018/api/clientes";
        private const int UsuarioId = 1;

        [ObservableProperty]
        private int _cantidadClientes;

        [ObservableProperty]
        private int _documentosEsteMes;

        [ObservableProperty]
        private int _presupuestosPendientes;

        public ObservableCollection<Documento> UltimosDocumentos { get; } = new();
        public ObservableCollection<Documento> ProximosAVencer { get; } = new();

        public ICommand NuevoPresupuestoCommand { get; }
        public ICommand NuevoRemitoCommand { get; }
        public ICommand VerHistorialCommand { get; }

        public InicioViewModel(ICommand irAPresupuesto, ICommand irARemito, ICommand irAHistorial)
        {
            NuevoPresupuestoCommand = irAPresupuesto;
            NuevoRemitoCommand = irARemito;
            VerHistorialCommand = irAHistorial;

            _httpClient = new HttpClient();

            Dispatcher.UIThread.Post(async () => await CargarDatosAsync());
        }

        private async Task CargarDatosAsync()
        {
            await Task.WhenAll(CargarClientesAsync(), CargarDocumentosAsync());
        }

        private async Task CargarClientesAsync()
        {
            try
            {
                var url = $"{ApiUrlClientes}?usuarioId={UsuarioId}";
                var clientes = await _httpClient.GetFromJsonAsync<List<Cliente>>(url);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    CantidadClientes = clientes?.Count ?? 0;
                });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[INICIO] Error al cargar clientes: {ex.Message}");
            }
        }

        private async Task CargarDocumentosAsync()
        {
            try
            {
                // Traemos todo el historial del usuario en una sola llamada;
                // de acá calculamos las 4 secciones de la pantalla.
                var url = $"{ApiUrlDocumentos}?usuarioId={UsuarioId}&tipoFiltro=0&buscarCliente=";
                var todos = await _httpClient.GetFromJsonAsync<List<Documento>>(url);

                if (todos == null) return;

                var ahora = DateTime.UtcNow;

                // Documentos este mes
                int esteMes = todos.Count(d => d.FechaEmision.Year == ahora.Year
                                             && d.FechaEmision.Month == ahora.Month);

                // Presupuestos pendientes (vigentes, aún sin respuesta del cliente)
                int pendientes = todos.Count(d => d is Presupuesto p && p.Estado == EstadoPresupuesto.Vigente);

                // Últimos 5 (ya vienen ordenados desc. por fecha desde el backend)
                var ultimos5 = todos.Take(5).ToList();

                // Próximos a vencer: Presupuestos y NotasCredito vigentes,
                // con vencimiento dentro de las próximas 24hs (inclusive)
                var limite = ahora.AddHours(24);
                var proximos = todos
                    .Where(d => d.FechaVencimientoAsociada.HasValue
                            && d.FechaVencimientoAsociada.Value >= ahora
                            && d.FechaVencimientoAsociada.Value <= limite
                            && ((d is Presupuesto p && p.Estado == EstadoPresupuesto.Vigente)
                                || (d is NotaCredito n && n.Estado == EstadoNotaCredito.Vigente)))
                    .OrderBy(d => d.FechaVencimientoAsociada)
                    .ToList();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    DocumentosEsteMes = esteMes;
                    PresupuestosPendientes = pendientes;

                    UltimosDocumentos.Clear();
                    foreach (var d in ultimos5) UltimosDocumentos.Add(d);

                    ProximosAVencer.Clear();
                    foreach (var d in proximos) ProximosAVencer.Add(d);
                });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[INICIO] Error al cargar documentos: {ex.Message}");
            }
        }
    }
}