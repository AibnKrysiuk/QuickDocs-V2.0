using System;

namespace QuickDocs.Core.Models
{
    // Hereda de Documento para obtener Id, Fecha, Talonario y Cliente
    public class NotaCredito : Documento
    {
        // El monto total del crédito a favor otorgado
        public decimal Total { get; set; }

        public EstadoNotaCredito Estado { get; set; } = EstadoNotaCredito.Vigente;

        public DateTime FechaVencimiento { get; set; }

        // Propiedad calculada para validar si caducó sin usar espacio en la base de datos
        public bool EstaVencida => Estado == EstadoNotaCredito.Vigente 
                                   && DateTime.UtcNow > FechaVencimiento;

        // Espacio libre para justificar el motivo del saldo a favor
        public string Detalle { get; set; } = string.Empty;
    }
}