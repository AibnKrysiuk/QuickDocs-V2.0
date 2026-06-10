using System.ComponentModel.DataAnnotations;
using QuickDocs.Core.Models; // Asegúrate de importar el namespace donde está tu Enum MetodoPago

namespace QuickDocs.Backend.Dtos
{
    public class ReciboCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }

        public int? ClienteId { get; set; }

        public string? ClienteNombreLibre { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El importe recibido debe ser mayor a 0")]
        public decimal ImporteRecibido { get; set; }

        public MetodoPago FormaPago { get; set; } = MetodoPago.Efectivo;

        [Required(ErrorMessage = "Debe especificar el detalle o concepto del recibo")]
        public string Detalle { get; set; } = string.Empty;

    }
}