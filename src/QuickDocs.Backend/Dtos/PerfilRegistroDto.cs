using Microsoft.AspNetCore.Http;

namespace QuickDocs.Backend.Dtos
{
    public class PerfilRegistroDto
    {
        public int UsuarioId { get; set; }
        public string NombreFantasia { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        
        // 🎯 NUEVO CAMPO: Localidad comercial para el formulario
        public string Localidad { get; set; } = string.Empty;
        
        public string CuitCuil { get; set; } = string.Empty;
        public string? TelefonoPrincipal { get; set; }
        public string? TelefonoSecundario { get; set; }
        public string EmailContacto { get; set; } = string.Empty;
        public string CondicionIva { get; set; } = string.Empty;
        
        // Aquí viajará la imagen del logo seleccionada desde la UI
        public IFormFile? LogoArchivo { get; set; }
    }
}