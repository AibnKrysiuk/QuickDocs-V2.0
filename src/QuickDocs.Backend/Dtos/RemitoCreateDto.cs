using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuickDocs.Backend.Dtos
{
    public class RemitoCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }

        public int? ClienteId { get; set; }
        public string? ClienteNombreLibre { get; set; }

        [RegularExpression(@"^$|^\d{11}$", ErrorMessage = "El CUIT/CUIL debe tener exactamente 11 dígitos, o dejarse vacío")]
        public string? ClienteCuitLibre { get; set; }

        public string DireccionEntrega { get; set; } = string.Empty;

        public decimal DescuentoGeneral { get; set; }

        // Opcional: Por si este remito nace a partir de un presupuesto existente
        public int? PresupuestoId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe cargar al menos un ítem")]
        public List<RemitoDetalleDto> Detalles { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool tieneClienteDeBase = ClienteId.HasValue && ClienteId.Value > 0;
            bool tieneClienteLibre = !string.IsNullOrWhiteSpace(ClienteNombreLibre);

            if (!tieneClienteDeBase && !tieneClienteLibre)
            {
                yield return new ValidationResult(
                    "Debe seleccionar un cliente de la base o escribir un nombre.",
                    new[] { nameof(ClienteId), nameof(ClienteNombreLibre) });
            }
        }
    }

    public class RemitoDetalleDto
    {
        public int? ItemId { get; set; }

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [Range(0.001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Cantidad { get; set; }
    }
}