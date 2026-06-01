using Avalonia.Controls;
using QuickDocs.UI.ViewModels;

namespace QuickDocs.UI.Views
{
    public partial class ArticulosView : UserControl
    {
        public ArticulosView()
        {
            InitializeComponent();
            DataContext = new ArticulosViewModel();
        }
    }
}