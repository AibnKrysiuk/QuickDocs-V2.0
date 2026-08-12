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

    [ObservableProperty]
    private string _vistaActual = "Inicio";
    
    [ObservableProperty]
    private bool _menuDocumentosExpandido;

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
        VistaActual = "Inicio";
        MenuDocumentosExpandido = false;
        var inicioVM = new InicioViewModel(MostrarPresupuesto, MostrarRemito, MostrarHistorial);
        ContenidoActual = new InicioView { DataContext = inicioVM };
    }

    private void EjecutarMostrarHistorial()
    {
        VistaActual = "Historial";
        MenuDocumentosExpandido = false;
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
                    VistaActual = "Remito";
                    MenuDocumentosExpandido = true;
                    var remitoVM = new RemitoViewModel();
                    await remitoVM.CargarRemitoExistente(docAEditar);
                    ContenidoActual = new RemitoView { DataContext = remitoVM };
                    break;

                case TipoDocumento.Recibo:
                    VistaActual = "Recibo";
                    MenuDocumentosExpandido = true;
                    var reciboVM = new ReciboViewModel();
                    await reciboVM.CargarReciboExistente(docAEditar);
                    ContenidoActual = new ReciboView { DataContext = reciboVM };
                    break;

                case TipoDocumento.NotaCredito:
                    VistaActual = "NotaCredito";
                    MenuDocumentosExpandido = true;
                    var notaCreditoVM = new NotaCreditoViewModel();
                    await notaCreditoVM.CargarNotaCreditoExistente(docAEditar);
                    ContenidoActual = new NotaCreditoView { DataContext = notaCreditoVM };
                    break;

                case TipoDocumento.Presupuesto:
                default:
                    VistaActual = "Presupuesto";
                    MenuDocumentosExpandido = true;
                    var presupuestoVM = new PresupuestoViewModel();
                    await presupuestoVM.CargarPresupuestoExistente(docAEditar);
                    ContenidoActual = new PresupuestoView { DataContext = presupuestoVM };
                    break;
            }
        });
    }

    private void EjecutarMostrarPresupuesto()
    {
        VistaActual = "Presupuesto";
        MenuDocumentosExpandido = true;
        ContenidoActual = new PresupuestoView();
    }

    private void EjecutarMostrarPerfil()
    {
        VistaActual = "Perfil";
        MenuDocumentosExpandido = false;
        ContenidoActual = new PerfilView();
    }

    private void EjecutarMostrarClientes()
    {
        VistaActual = "Clientes";
        MenuDocumentosExpandido = false;
        ContenidoActual = new ClientesView();
    }

    private void EjecutarMostrarArticulos()
    {
        VistaActual = "Articulos";
        MenuDocumentosExpandido = false;
        ContenidoActual = new ArticulosView();
    }

    private void EjecutarMostrarRecibo()
    {
        VistaActual = "Recibo";
        MenuDocumentosExpandido = true;
        ContenidoActual = new ReciboView();
    }

    private void EjecutarMostrarRemito()
    {
        VistaActual = "Remito";
        MenuDocumentosExpandido = true;
        ContenidoActual = new RemitoView();
    }

    private void EjecutarMostrarNotaCredito()
    {
        VistaActual = "NotaCredito";
        MenuDocumentosExpandido = true;
        ContenidoActual = new NotaCreditoView();
    }

    partial void OnMenuDocumentosExpandidoChanged(bool value)
    {
        // Si se abrió y la vista actual NO es uno de los 4 tipos de documento,
        // significa que fue un toggle manual del usuario (no una navegación nuestra) 
        // -> apagamos el resaltado del item de arriba.
        bool esUnTipoDeDocumento = VistaActual is "Presupuesto" or "Remito" or "Recibo" or "NotaCredito";

        if (value && !esUnTipoDeDocumento)
        {
            VistaActual = string.Empty;
        }
    }
}