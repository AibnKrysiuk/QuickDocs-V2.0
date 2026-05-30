namespace QuickDocs.Core.Models
{
    public class Item
    {
        public int Id { get; set; }

        // El dueño de este producto/servicio
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public TipoItem Tipo { get; set; } = TipoItem.Producto;
        
        // Campos opcionales (el '?' permite que sean nulos)
        public string? Marca { get; set; } 
        public string? UnidadMedida { get; set; } // Ej: "Unidad", "Kg"
        
    }
}