using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuickDocs.Backend.Dtos
{
    public class RemitoCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }

        public int? ClienteId { get; set; }

        [Required(ErrorMessage = "La dirección de entrega es obligatoria para el remito")]
        public string DireccionEntrega { get; set; } = string.Empty;

        public decimal DescuentoGeneral { get; set; }

        // Opcional: Por si este remito nace a partir de un presupuesto existente
        public int? PresupuestoId { get; set; }

        [Required]
        public List<RemitoDetalleDto> Detalles { get; set; } = new();
    }

    public class RemitoDetalleDto
    {
        [Required]
        public int ItemId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Cantidad { get; set; }
    }
}