using System;

namespace QuickDocs.Desktop.Services
{
    public static class SessionManager
    {
        // Guardamos el token JWT
        public static string? Token { get; private set; }
        
        // Guardamos el ID del usuario por si necesitamos consultar datos específicos de él
        public static int? UsuarioId { get; private set; }
        
        // Guardamos el nombre para poder mostrar un cartel de "¡Bienvenido, Admin!" en la UI
        public static string? Username { get; private set; }

        /// <summary>
        /// Registra los datos de la sesión activa al loguearse con éxito.
        /// </summary>
        public static void IniciarSesion(string token, int usuarioId, string username)
        {
            Token = token;
            UsuarioId = usuarioId;
            Username = username;
        }

        /// <summary>
        /// Borra los datos de la memoria cuando el usuario cierra sesión.
        /// </summary>
        public static void CerrarSesion()
        {
            Token = null;
            UsuarioId = null;
            Username = null;
        }

        /// <summary>
        /// Nos permite saber de forma rápida en cualquier parte de la app si hay alguien logueado.
        /// </summary>
        public static bool EstaLogueado => !string.IsNullOrEmpty(Token);
    }
}