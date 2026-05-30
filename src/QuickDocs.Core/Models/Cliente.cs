using System;

namespace QuickDocs.Core.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        
        // El dueño de este registro de cliente
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        // Datos personales /Empresa
        public string Nombre {get; set; } = string.Empty;
        public string CuitCuil { get; set; } = string.Empty;

        // Datos de Contacto
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Localidad { get; set; }

        // Auditoría y Relación (Importante para la v2.0)
        public DateTime FechaAlta { get; set; } = DateTime.Now;
        
    }
}