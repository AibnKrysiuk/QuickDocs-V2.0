using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickDocs.Desktop.Views;

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
            ContenidoActual = new HistorialView();
        }
        private void EjecutarMostrarPerfil() // 👈 Agregado
        {
            ContenidoActual = new PerfilView();
        }
        private void EjecutarMostrarPresupuesto() 
        {
            ContenidoActual = new PresupuestoView();
        }
        private void EjecutarMostrarClientes() // 👈 Nuevo
        {
            ContenidoActual = new ClientesView();
        }
        private void EjecutarMostrarArticulos()
        {
            ContenidoActual = new ArticulosView();
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