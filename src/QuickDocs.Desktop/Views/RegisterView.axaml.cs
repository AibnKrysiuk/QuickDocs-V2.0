using Avalonia.Controls;
using Avalonia.Interactivity;
using QuickDocs.Desktop.ViewModels;

namespace QuickDocs.Desktop.Views
{
    public partial class RegisterView : Window
    {
        public RegisterView()
        {
            InitializeComponent();
            this.DataContext = new LoginViewModel();
        }

        // Acción al hacer clic en "Registrar"
        private async void OnRegistrarClick(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is LoginViewModel viewModel)
            {
                // Validamos rápido antes de gastar recursos de red
                if (string.IsNullOrWhiteSpace(viewModel.Usuario) || string.IsNullOrWhiteSpace(viewModel.Contrasena))
                {
                    viewModel.MensajeError = "Por favor, completa todos los campos.";
                    return;
                }

                if (sender is Button btn) btn.IsEnabled = false;
                viewModel.MensajeError = string.Empty;

                // Instanciamos el servicio y le pegamos a la API
                var authService = new QuickDocs.Desktop.Services.AuthService();
                bool registrado = await authService.RegisterAsync(viewModel.Usuario, viewModel.Contrasena);

                if (registrado)
                {
                    // ¡Éxito! Limpiamos campos y mandamos al usuario de vuelta al Login para que use su cuenta
                    var loginView = new LoginView();
                    loginView.Show();
                    this.Close();
                }
                else
                {
                    // Error (Usuario duplicado o API caída)
                    viewModel.MensajeError = "No se pudo registrar. ¿El usuario ya existe?";
                    if (sender is Button btnRecuperado) btnRecuperado.IsEnabled = true;
                }
            }
        }

        // Navegación de vuelta al Login
        private void OnVolverClick(object sender, RoutedEventArgs e)
        {
            var loginView = new LoginView();
            loginView.Show();
            this.Close(); // Cerramos la ventana de registro
        }
    }
}