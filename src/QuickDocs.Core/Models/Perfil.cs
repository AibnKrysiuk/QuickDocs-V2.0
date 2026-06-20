using System;

namespace QuickDocs.Core.Models
{
    public class Perfil
    {
        public int Id { get; set; }

        // El ancla con el Usuario (Clave Foránea)
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        // Datos de Identidad Comercial (Para el membrete del PDF)
        public string NombreFantasia { get; set; } = string.Empty;
        public string CuitCuil { get; set; } = string.Empty; // 🎯 NUEVO CAMPO: CUIT / CUIL fiscal
        public string Direccion { get; set; } = string.Empty;
        public string Localidad { get; set; } = string.Empty;
        public string? TelefonoPrincipal { get; set; }
        public string? TelefonoSecundario { get; set; }
        public string EmailContacto { get; set; } = string.Empty;
        
        // Datos fiscales requeridos en Argentina (Monotributo, Consumidor Final, etc.)
        public string CondicionIva { get; set; } = string.Empty;

        // Ruta del archivo de imagen del logo en el disco local
        public string LogoPath { get; set; } = string.Empty;
    }
}