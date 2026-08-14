using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuickDocs.Backend.Dtos
{
    public class PresupuestoCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }

        public int? ClienteId { get; set; }
        public string? ClienteNombreLibre { get; set; }
        
        //Solo 11 digitos ó vacío
        [RegularExpression(@"^$|^\d{11}$", ErrorMessage = "El CUIT/CUIL debe tener exactamente 11 dígitos, o dejarse vacío")]
        public string? ClienteCuitLibre { get; set; }
        public string? ClienteDireccionLibre { get; set; }

        [Range(1, 99, ErrorMessage = "Los días de validez deben estar entre 1 y 99")]
        public int DiasValidez { get; set; } = 15;

        public double DescuentoGeneral { get; set; }

        [MaxLength(150, ErrorMessage = "El motivo del descuento no puede superar los 150 caracteres")]
        public string? MotivoDescuento { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe cargar al menos un ítem")]
        public List<PresupuestoDetalleDto> Detalles { get; set; } = new List<PresupuestoDetalleDto>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            decimal subtotalCalculado = 0m;
            foreach (var d in Detalles)
                subtotalCalculado += d.Precio * d.Cantidad;

            if ((decimal)DescuentoGeneral > subtotalCalculado)
            {
                yield return new ValidationResult(
                    "El descuento no puede ser mayor al subtotal del presupuesto.",
                    new[] { nameof(DescuentoGeneral) });
            }

            if (DescuentoGeneral < 0)
            {
                yield return new ValidationResult(
                    "El descuento no puede ser negativo.",
                    new[] { nameof(DescuentoGeneral) });
            }
        }
    }

    public class PresupuestoDetalleDto
    {
        // 🔓 Quitamos [Required] y lo hacemos anulable para permitir ítems libres
        public int? ItemId { get; set; }

        // 📝 Agregamos la descripción y precio al DTO para capturar lo que se tipeó en el formulario
        [Required]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo.")]
        public decimal Precio { get; set; }

        [Required]
        [Range(0.001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Cantidad { get; set; }
    }
}