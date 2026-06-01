using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuickDocs.Backend.Dtos
{
    public class PresupuestoCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }

        public int? ClienteId { get; set; }

        public double DescuentoGeneral { get; set; }

        [Required]
        public List<PresupuestoDetalleDto> Detalles { get; set; } = new List<PresupuestoDetalleDto>();
    }

    public class PresupuestoDetalleDto
    {
        // 🔓 Quitamos [Required] y lo hacemos anulable para permitir ítems libres
        public int? ItemId { get; set; }

        // 📝 Agregamos la descripción y precio al DTO para capturar lo que se tipeó en el formulario
        [Required]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Cantidad { get; set; }
    }
}