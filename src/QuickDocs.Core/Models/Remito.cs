using System;
using System.Collections.Generic;

namespace QuickDocs.Core.Models
{
    public class Remito : Documento
    {
        // Vínculo opcional al presupuesto de origen (null si se creó desde cero)
        public int? PresupuestoId { get; set; }

        // El campo clave que acabás de marcar: obligatorio para el reparto
        public string DireccionEntrega { get; set; } = string.Empty;

        public EstadoRemito Estado { get; set; } = EstadoRemito.Vigente;
        
        // DateTime? permite que sea nula hasta que se entregue efectivamente
        public DateTime? FechaEntrega { get; set; } 

        // Totales financieros para control interno
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }

        public List<DetalleRemito> Detalles { get; set; } = new();
    }
}