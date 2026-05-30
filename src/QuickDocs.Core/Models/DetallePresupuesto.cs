namespace QuickDocs.Core.Models
{
    public class DetallePresupuesto
    {
        public int Id { get; set; }
        
        // El ancla con el presupuesto padre
        public int PresupuestoId { get; set; }

        // El vínculo con el Ítem original
        public int? ItemId { get; set; }
        public Item? Item { get; set; }

        // Copia de seguridad del nombre del producto/servicio
        public string DescripcionSnapshot { get; set; } = string.Empty; 
        
        // Cantidad y Precio del momento
        public decimal Cantidad { get; set; }
        public decimal PrecioAplicado { get; set; } 

        // Propiedad calculada: da el total de este renglón automáticamente
        public decimal Importe => Cantidad * PrecioAplicado; 
    }
}