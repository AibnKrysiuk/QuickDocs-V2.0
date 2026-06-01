using Avalonia.Controls;
using QuickDocs.UI.ViewModels;

namespace QuickDocs.UI.Views
{
    public partial class HistorialView : UserControl
    {
        public HistorialView()
        {
            InitializeComponent();
            DataContext = new HistorialViewModel();
        }
    }
}