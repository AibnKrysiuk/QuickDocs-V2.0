using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using QuickDocs.Desktop.Services; // Para usar AuthService y SessionManager

namespace QuickDocs.Desktop.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService = new AuthService();

        private string _usuario = string.Empty;
        public string Usuario
        {
            get => _usuario;
            set { _usuario = value; OnPropertyChanged(); }
        }

        private string _contrasena = string.Empty;
        public string Contrasena
        {
            get => _contrasena;
            set { _contrasena = value; OnPropertyChanged(); }
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set { _mensajeError = value; OnPropertyChanged(); }
        }

        private bool _recordarSesion;
        public bool RecordarSesion
        {
            get => _recordarSesion;
            set { _recordarSesion = value; OnPropertyChanged(); }
        }

        // Evento para avisar a la vista que el login fue exitoso
        public event Action? OnLoginExitoso;

        public LoginViewModel()
        {
            // Ya no inicializamos ningún comando manual acá
        }

        /// <summary>
        /// Este método será llamado directamente por el botón del XAML
        /// </summary>
        public async Task EjecutarLoginAsync()
        {
            MensajeError = string.Empty;

            if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Contrasena))
            {
                MensajeError = "Por favor, completa todos los campos.";
                return;
            }

            // Llamada real a la API
            var resultado = await _authService.LoginAsync(Usuario, Contrasena);

            if (resultado != null)
            {
                // Guardamos el token en la sesión privada de la app
                SessionManager.IniciarSesion(resultado.Token, resultado.UsuarioId, resultado.Username);
                
                // Avisamos a la vista para cambiar de ventana
                OnLoginExitoso?.Invoke();
            }
            else
            {
                MensajeError = "Usuario o contraseña incorrectos.";
            }
        }

        // Implementación estándar de notificación para Avalonia
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}