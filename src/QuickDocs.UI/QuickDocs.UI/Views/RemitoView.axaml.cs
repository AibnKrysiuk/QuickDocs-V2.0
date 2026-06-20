using Avalonia.Controls;
using QuickDocs.UI.ViewModels;
using System.Net.Http; // Asegurar el using

namespace QuickDocs.UI.Views
{
    public partial class RemitoView : UserControl
    {
        // Si tu vista recibe el HttpClient o usás un contenedor global, lo pasamos acá
        public RemitoView()
        {
            InitializeComponent();
            DataContext = new RemitoViewModel();
        }

        private void DeseleccionarLista_OnPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (DataContext is QuickDocs.UI.ViewModels.RemitoViewModel vm)
            {
                vm.DetalleSeleccionado = null;
            }
        }

    }
}