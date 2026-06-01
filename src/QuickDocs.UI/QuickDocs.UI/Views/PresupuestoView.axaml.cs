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
    }
}