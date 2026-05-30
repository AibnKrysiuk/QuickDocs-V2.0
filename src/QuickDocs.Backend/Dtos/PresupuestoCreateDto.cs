using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuickDocs.Backend.Dtos
{
    // El molde principal de la Cabecera que viene desde afuera
    public class PresupuestoCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }

        public int? ClienteId { get; set; }

        public double DescuentoGeneral { get; set; } // Por si tu tía quiere hacer un descuento al total

        [Required]
        public List<PresupuestoDetalleDto> Detalles { get; set; } = new List<PresupuestoDetalleDto>();
    }

    // El molde de cada Renglón (Detalle) que viene en la lista
    public class PresupuestoDetalleDto
    {
        [Required]
        public int ItemId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Cantidad { get; set; } // ¡Cambiado de int a decimal!
    }
}