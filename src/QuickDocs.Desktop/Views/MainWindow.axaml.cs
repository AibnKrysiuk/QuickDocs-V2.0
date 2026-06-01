using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using QuickDocs.Desktop.ViewModels;
using QuickDocs.UI.ViewModels;
using QuickDocs.UI.Views;
using System;

namespace QuickDocs.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            var mainVm = new MainWindowViewModel();
            DataContext = mainVm;

            // 🛠️ DETECTOR DINÁMICO DE PESTAÑAS
            // Escuchamos cada vez que cambie el contenido del panel derecho
            mainVm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.ContenidoActual))
                {
                    // Si la vista activa es un UserControl y su DataContext es el del Historial...
                    if (mainVm.ContenidoActual is UserControl userControl 
                        && userControl.DataContext is HistorialViewModel historialVm)
                    {
                        // Evitamos que se dupliquen hilos
                        historialVm.OnSolicitarModificacion = null;

                        // 🪝 Enganchamos la acción del botón Editar
                        // 🪝 Enganchamos la acción del botón Editar (agregamos async aquí)
                        historialVm.OnSolicitarModificacion = async (presupuestoViejo) =>
                        {
                            System.Console.WriteLine($"[MAIN] ¡Puente activado! Recibido ID: {presupuestoViejo.Id}");

                            // Creamos la pantalla de destino nueva
                            var presupuestoView = new PresupuestoView();
                            if (presupuestoView.DataContext is PresupuestoViewModel presupuestoVm)
                            {
                                // 🔥 MODIFICADO: Agregamos await aquí para esperar la sincronización
                                await presupuestoVm.CargarPresupuestoExistente(presupuestoViejo);
                                
                                // Forzamos el salto visual en el panel derecho
                                mainVm.ContenidoActual = presupuestoView;
                            }
                        };
                    }
                }
            };
        }

        // Lógica para el botón Salir (se mantiene intacta)
        private void BotonSalir_Click(object? sender, RoutedEventArgs e)
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
}