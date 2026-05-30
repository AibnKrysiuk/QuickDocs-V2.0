namespace QuickDocs.Core.Models
{
    public class DetalleRemito
    {
        public int Id { get; set; }
        
        // El ancla con el remito padre
        public int RemitoId { get; set; }

        // El vínculo con el Ítem original (opcional por si es carga manual)
        public int? ItemId { get; set; }
        public Item? Item { get; set; }

        public string DescripcionSnapshot { get; set; } = string.Empty; 
        
        public decimal Cantidad { get; set; }
        public decimal PrecioAplicado { get; set; } 

        public decimal Importe => Cantidad * PrecioAplicado; 
    }
}