using System;

namespace QuickDocs.Core.Models
{
    // Hereda de Documento para tener Id, Fecha, Talonario y Cliente
    public class Recibo : Documento
    {
        // El monto de dinero que ingresa
        public decimal ImporteRecibido { get; set; }

        // Cómo se pagó (Efectivo, Transferencia, etc.)
        public MetodoPago FormaPago { get; set; } = MetodoPago.Efectivo;

        // Texto libre para aclarar el motivo del cobro
        public string Detalle { get; set; } = string.Empty;

        // Vínculo opcional por si el cobro viene de un Remito específico
        public int? RemitoId { get; set; }
    }
}