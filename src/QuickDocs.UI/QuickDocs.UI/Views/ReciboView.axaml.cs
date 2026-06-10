using Avalonia.Controls;
using QuickDocs.UI.ViewModels;

namespace QuickDocs.UI.Views
{
    public partial class ReciboView : UserControl
    {
        public ReciboView()
        {
            InitializeComponent();
            DataContext = new ReciboViewModel();
        }
    }
}