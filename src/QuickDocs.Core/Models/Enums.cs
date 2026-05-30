namespace QuickDocs.Core.Models
{
    public enum MetodoPago
    {
        Efectivo,
        Qr,
        Transferencia,
        TarjetaDebito,
        TarjetaCredito,
        Cheque
    }

    public enum TipoItem 
    { 
        Producto, 
        Servicio 
    }    

    public enum TipoDocumento
    {
        Presupuesto,
        Remito,
        Recibo,
        NotaCredito
    }

    public enum EstadoPresupuesto
    {
        Vigente,
        Aceptado,
        Vencido,
        Rechazado
    }

    public enum EstadoRemito
    {
        Vigente,
        Entregado,
        Rechazado
    }

    public enum EstadoNotaCredito
    {
        Vigente,
        Utilizada,
        Vencida
    }
}
