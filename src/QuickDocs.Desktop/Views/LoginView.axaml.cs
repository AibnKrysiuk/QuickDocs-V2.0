using Avalonia.Controls;
using Avalonia.Interactivity;
using QuickDocs.Desktop.ViewModels;

namespace QuickDocs.Desktop.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();

            // INSTANCIAMOS Y ASIGNAMOS EL DATACONTEXT ACÁ SÍ O SÍ
            var viewModel = new LoginViewModel();
            this.DataContext = viewModel;

            // Nos suscribimos al evento del éxito
            viewModel.OnLoginExitoso += () =>
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            };
        }

        // Este método captura el clic del botón y puentea de forma asincrónica al ViewModel
        private async void OnIngresarClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                // Deshabilitamos el botón temporalmente desde el emisor del evento para evitar el doble clic
                if (sender is Button btn) btn.IsEnabled = false;

                // Ejecutamos la lógica de la API
                await viewModel.EjecutarLoginAsync();

                // Volvemos a habilitar el botón por si las credenciales fallaron y debe reintentar
                if (sender is Button btnRecuperado) btnRecuperado.IsEnabled = true;
            }
        }

        private void OnIrARegistroClick(object sender, RoutedEventArgs e)
        {
            var registerView = new RegisterView();
            registerView.Show();
            this.Close(); // Cerramos el Login para dejar limpia la pantalla
        }
    }
}