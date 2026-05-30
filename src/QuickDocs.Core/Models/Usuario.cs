using System;

namespace QuickDocs.Core.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        
        // Credenciales de acceso
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        // Contraseña encriptada por seguridad
        public string PasswordHash { get; set; } = string.Empty;

        // Control de estado de la cuenta
        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Propiedad de navegación: Relación 1 a 1 con su Perfil Comercial
        public Perfil? Perfil { get; set; }
    }
}