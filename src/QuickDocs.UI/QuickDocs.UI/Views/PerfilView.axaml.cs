using Avalonia.Controls;
using QuickDocs.UI.ViewModels;

namespace QuickDocs.UI.Views
{
    public partial class PerfilView : UserControl
    {
        public PerfilView()
        {
            InitializeComponent();

            DataContext = new PerfilViewModel();
        }
    }
}