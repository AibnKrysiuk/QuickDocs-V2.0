using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using QuickDocs.Desktop.ViewModels;
using QuickDocs.UI.ViewModels;
using QuickDocs.UI.Views;
using QuickDocs.Core.Models; // 🎯 Aseguramos el acceso a TipoDocumento
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
            mainVm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.ContenidoActual))
                {
                    if (mainVm.ContenidoActual is UserControl userControl 
                        && userControl.DataContext is HistorialViewModel historialVm)
                    {
                        historialVm.OnSolicitarModificacion = null;

                        historialVm.OnSolicitarModificacion = async (docViejo) =>
                        {
                            System.Console.WriteLine($"[MAIN] ¡Puente activado! Recibido ID: {docViejo.Id}, Tipo: {docViejo.Tipo}");

                            // 🎯 CAMBIO: Evaluamos todos los tipos de documento para abrir la vista correcta
                            switch (docViejo.Tipo)
                            {
                                case TipoDocumento.Remito:
                                    var remitoView = new RemitoView();
                                    if (remitoView.DataContext is RemitoViewModel remitoVm)
                                    {
                                        await remitoVm.CargarRemitoExistente(docViejo);
                                        mainVm.ContenidoActual = remitoView;
                                    }
                                    break;

                                case TipoDocumento.Recibo:
                                    var reciboView = new ReciboView();
                                    if (reciboView.DataContext is ReciboViewModel reciboVm)
                                    {
                                        await reciboVm.CargarReciboExistente(docViejo);
                                        mainVm.ContenidoActual = reciboView;
                                    }
                                    break;

                                case TipoDocumento.NotaCredito:
                                    var notaCreditoView = new NotaCreditoView();
                                    if (notaCreditoView.DataContext is NotaCreditoViewModel notaCreditoVm)
                                    {
                                        await notaCreditoVm.CargarNotaCreditoExistente(docViejo);
                                        mainVm.ContenidoActual = notaCreditoView;
                                    }
                                    break;

                                case TipoDocumento.Presupuesto:
                                default:
                                    var presupuestoView = new PresupuestoView();
                                    if (presupuestoView.DataContext is PresupuestoViewModel presupuestoVm)
                                    {
                                        await presupuestoVm.CargarPresupuestoExistente(docViejo);
                                        mainVm.ContenidoActual = presupuestoView;
                                    }
                                    break;
                            }
                        };
                    }
                }
            };
        }

        private void BotonSalir_Click(object? sender, RoutedEventArgs e)
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
}