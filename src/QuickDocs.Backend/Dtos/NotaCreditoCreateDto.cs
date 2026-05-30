using System.ComponentModel.DataAnnotations;

namespace QuickDocs.Backend.Dtos
{
    public class NotaCreditoCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }

        public int? ClienteId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El total de la nota de crédito debe ser mayor a 0")]
        public decimal Total { get; set; }

        [Required(ErrorMessage = "Debe especificar el motivo o detalle de la nota de crédito")]
        public string Detalle { get; set; } = string.Empty;
    }
}