using Avalonia.Controls;
using QuickDocs.UI.ViewModels;

namespace QuickDocs.UI.Views
{
    public partial class ClientesView : UserControl
    {
        public ClientesView()
        {
            InitializeComponent();
            
            // 🧠 Le asignamos su ViewModel correspondiente como DataContext
            DataContext = new ClientesViewModel();
        }
    }
}