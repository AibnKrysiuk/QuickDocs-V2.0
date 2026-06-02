using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickDocs.Desktop.Views;
using QuickDocs.UI.Views;

namespace QuickDocs.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private object? _contenidoActual;

        // 1. Declaramos la propiedad explícita que tu XAML está buscando: Command="{Binding MostrarHistorial}"
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
            // Vinculamos los comandos a sus respectivos métodos
            MostrarInicio = new RelayCommand(EjecutarMostrarInicio);
            MostrarHistorial = new RelayCommand(EjecutarMostrarHistorial);
            MostrarPerfil = new RelayCommand(EjecutarMostrarPerfil);
            MostrarPresupuesto = new RelayCommand(EjecutarMostrarPresupuesto);
            
            MostrarClientes = new RelayCommand(EjecutarMostrarClientes);
            MostrarArticulos = new RelayCommand(EjecutarMostrarArticulos);
            MostrarRecibo = new RelayCommand(EjecutarMostrarRecibo);
            MostrarRemito = new RelayCommand(EjecutarMostrarRemito);
            MostrarNotaCredito = new RelayCommand(EjecutarMostrarNotaCredito);
            

            // 🔥 CLAVE: Seteamos la pantalla de inicio por defecto al arrancar
            EjecutarMostrarInicio();
        }

        private void EjecutarMostrarInicio()
        {
            ContenidoActual = new InicioView();
        }
        private void EjecutarMostrarHistorial()
        {
            // Instancia limpia estándar
            var vistaHistorial = new QuickDocs.UI.Views.HistorialView();

            if (vistaHistorial.DataContext is QuickDocs.UI.ViewModels.HistorialViewModel historialVM)
            {
                historialVM.OnSolicitarModificacion += (presupuestoAEditar) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var presupuestoVM = new QuickDocs.UI.ViewModels.PresupuestoViewModel();
                        presupuestoVM.CargarPresupuestoExistente(presupuestoAEditar);

                        var vistaPresupuesto = new QuickDocs.UI.Views.PresupuestoView
                        {
                            DataContext = presupuestoVM
                        };

                        ContenidoActual = vistaPresupuesto;
                    });
                };
            }

            ContenidoActual = vistaHistorial;
        }

        private void EjecutarMostrarPresupuesto()
        {
            // Al ir directo, simplemente mostramos la vista limpia
            ContenidoActual = new QuickDocs.UI.Views.PresupuestoView(); 
        }        private void EjecutarMostrarPerfil() // 👈 Agregado
        {
            ContenidoActual = new QuickDocs.UI.Views.PerfilView();
        }
        private void EjecutarMostrarClientes() // 👈 Nuevo
        {
            ContenidoActual = new QuickDocs.UI.Views.ClientesView();
        }
        private void EjecutarMostrarArticulos()
        {
            // 🚀 Cambiamos para usar la vista de la capa compartida UI
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
        private void EjecutarMostrarNotaCredito() // 👈 Agregado
        {
            ContenidoActual = new NotaCreditoView();
        }
    }
}