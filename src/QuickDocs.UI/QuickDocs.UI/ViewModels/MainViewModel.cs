using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickDocs.UI.Views;
using QuickDocs.Core.Models;

namespace QuickDocs.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
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

    public MainViewModel()
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
        var inicioVM = new InicioViewModel(MostrarPresupuesto, MostrarRemito, MostrarHistorial);
        ContenidoActual = new InicioView { DataContext = inicioVM };
    }

    private void EjecutarMostrarHistorial()
    {
        var vistaHistorial = new HistorialView();

        if (vistaHistorial.DataContext is HistorialViewModel historialVM)
        {
            historialVM.OnSolicitarModificacion += (docAEditar) =>
            {
                NavegarADocumentoDirecto(docAEditar);
            };
        }
        ContenidoActual = vistaHistorial;
    }

    public void NavegarADocumentoDirecto(Documento docAEditar)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            switch (docAEditar.Tipo)
            {
                case TipoDocumento.Remito:
                    var remitoVM = new RemitoViewModel();
                    await remitoVM.CargarRemitoExistente(docAEditar);
                    ContenidoActual = new RemitoView { DataContext = remitoVM };
                    break;

                case TipoDocumento.Recibo:
                    var reciboVM = new ReciboViewModel();
                    await reciboVM.CargarReciboExistente(docAEditar);
                    ContenidoActual = new ReciboView { DataContext = reciboVM };
                    break;

                case TipoDocumento.NotaCredito:
                    var notaCreditoVM = new NotaCreditoViewModel();
                    await notaCreditoVM.CargarNotaCreditoExistente(docAEditar);
                    ContenidoActual = new NotaCreditoView { DataContext = notaCreditoVM };
                    break;

                case TipoDocumento.Presupuesto:
                default:
                    var presupuestoVM = new PresupuestoViewModel();
                    await presupuestoVM.CargarPresupuestoExistente(docAEditar);
                    ContenidoActual = new PresupuestoView { DataContext = presupuestoVM };
                    break;
            }
        });
    }

    private void EjecutarMostrarPresupuesto()
    {
        ContenidoActual = new PresupuestoView();
    }

    private void EjecutarMostrarPerfil()
    {
        ContenidoActual = new PerfilView();
    }

    private void EjecutarMostrarClientes()
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

    private void EjecutarMostrarNotaCredito()
    {
        ContenidoActual = new NotaCreditoView();
    }
}