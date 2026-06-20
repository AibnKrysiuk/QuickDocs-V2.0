using Avalonia.Controls;
using QuickDocs.UI.ViewModels;

namespace QuickDocs.UI.Views
{
    public partial class PresupuestoView : UserControl
    {
        public PresupuestoView()
        {
            InitializeComponent();
            DataContext = new PresupuestoViewModel();
        }

        private void DeseleccionarLista_OnPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (DataContext is QuickDocs.UI.ViewModels.PresupuestoViewModel vm)
            {
                vm.DetalleSeleccionado = null;
            }
        }
    }
}