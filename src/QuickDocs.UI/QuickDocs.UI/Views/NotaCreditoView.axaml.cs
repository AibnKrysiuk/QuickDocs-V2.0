using Avalonia.Controls;
using QuickDocs.UI.ViewModels; // Asegúrate de importar el namespace del ViewModel

namespace QuickDocs.UI.Views
{
    public partial class NotaCreditoView : UserControl
    {
        public NotaCreditoView()
        {
            InitializeComponent();
            // Asignamos el ViewModel como DataContext
            DataContext = new NotaCreditoViewModel();
        }
    }
}