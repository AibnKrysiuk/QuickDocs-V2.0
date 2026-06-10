using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickDocs.Desktop.Views;
using QuickDocs.UI.Views;
using QuickDocs.Core.Models;

namespace QuickDocs.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private object? _contenidoActual;

        public ICommand MostrarInicio { get; }
        public ICommand MostrarHistorial { get; }
        public ICommand MostrarPerfil { get; }
        public ICommand MostrarPresupuesto { get; }
        public ICommand MostrarRecibo { get; }
        public ICommand MostrarRemito { get; }
        public ICommand MostrarClientes { get; }
        public ICommand MostrarArticulos { get; }
        public ICommand MostrarNotaCredito { get; }

        public MainWindowViewModel()
        {
            MostrarInicio = new RelayCommand(EjecutarMostrarInicio);
            MostrarHistorial = new RelayCommand(EjecutarMostrarHistorial);
            MostrarPerfil = new RelayCommand(EjecutarMostrarPerfil);
            MostrarPresupuesto = new RelayCommand(EjecutarMostrarPresupuesto);
            
            MostrarClientes = new RelayCommand(EjecutarMostrarClientes);
            MostrarArticulos = new RelayCommand(EjecutarMostrarArticulos);
            MostrarRecibo = new RelayCommand(EjecutarMostrarRecibo);
            MostrarRemito = new RelayCommand(EjecutarMostrarRemito);
            MostrarNotaCredito = new RelayCommand(EjecutarMostrarNotaCredito);
            
            EjecutarMostrarInicio();
        }

        private void EjecutarMostrarInicio()
        {
            ContenidoActual = new InicioView();
        }

        private void EjecutarMostrarHistorial()
        {
            var vistaHistorial = new QuickDocs.UI.Views.HistorialView();

            if (vistaHistorial.DataContext is QuickDocs.UI.ViewModels.HistorialViewModel historialVM)
            {
                historialVM.OnSolicitarModificacion += (docAEditar) =>
                {
                    NavegarADocumentoDirecto(docAEditar);
                };
            }
            ContenidoActual = vistaHistorial;
        }

        // 🎯 NUEVO: Método centralizado para inyectar y mostrar cualquier documento existente de forma directa
        public void NavegarADocumentoDirecto(Documento docAEditar)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(async () => 
            {
                switch (docAEditar.Tipo)
                {
                    case QuickDocs.Core.Models.TipoDocumento.Remito:
                        var remitoVM = new QuickDocs.UI.ViewModels.RemitoViewModel();
                        await remitoVM.CargarRemitoExistente(docAEditar);
                        ContenidoActual = new QuickDocs.UI.Views.RemitoView { DataContext = remitoVM };
                        break;

                    case QuickDocs.Core.Models.TipoDocumento.Recibo:
                        var reciboVM = new QuickDocs.UI.ViewModels.ReciboViewModel();
                        await reciboVM.CargarReciboExistente(docAEditar); 
                        ContenidoActual = new QuickDocs.UI.Views.ReciboView { DataContext = reciboVM };
                        break;

                    case QuickDocs.Core.Models.TipoDocumento.NotaCredito:
                        var notaCreditoVM = new QuickDocs.UI.ViewModels.NotaCreditoViewModel();
                        await notaCreditoVM.CargarNotaCreditoExistente(docAEditar); 
                        ContenidoActual = new QuickDocs.UI.Views.NotaCreditoView { DataContext = notaCreditoVM };
                        break;

                    case QuickDocs.Core.Models.TipoDocumento.Presupuesto:
                    default:
                        var presupuestoVM = new QuickDocs.UI.ViewModels.PresupuestoViewModel();
                        await presupuestoVM.CargarPresupuestoExistente(docAEditar);
                        ContenidoActual = new QuickDocs.UI.Views.PresupuestoView { DataContext = presupuestoVM };
                        break;
                }
            });
        }

        private void EjecutarMostrarPresupuesto()
        {
            ContenidoActual = new QuickDocs.UI.Views.PresupuestoView(); 
        }

        private void EjecutarMostrarPerfil()
        {
            ContenidoActual = new QuickDocs.UI.Views.PerfilView();
        }

        private void EjecutarMostrarClientes()
        {
            ContenidoActual = new QuickDocs.UI.Views.ClientesView();
        }

        private void EjecutarMostrarArticulos()
        {
            ContenidoActual = new QuickDocs.UI.Views.ArticulosView();
        }

        private void EjecutarMostrarRecibo()
        {
            ContenidoActual = new ReciboView();
        }

        private void EjecutarMostrarRemito()
        {
            ContenidoActual = new RemitoView();
        }

        private void EjecutarMostrarNotaCredito()
        {
            ContenidoActual = new NotaCreditoView();
        }
    }
}