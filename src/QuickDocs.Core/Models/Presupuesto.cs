using System;
using System.Collections.Generic;

namespace QuickDocs.Core.Models
{
    // Usamos ': Documento' para heredar toda la estructura base
    public class Presupuesto : Documento
    {
        public DateTime FechaVencimiento { get; set; }
        
        // Totales de dinero específicos de esta transacción
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }

        // El estado usando el Enum exclusivo de Presupuestos que creamos antes
        public EstadoPresupuesto Estado { get; set; } = EstadoPresupuesto.Vigente;

        // Propiedad calculada al vuelo para saber si caducó (no ocupa espacio en la tabla)
        public bool EstaVencido => Estado == EstadoPresupuesto.Vigente 
                                   && DateTime.UtcNow > FechaVencimiento;

        // La lista con los renglones (Detalles) que validamos recién
        public List<DetallePresupuesto> Detalles { get; set; } = new();
    }
}