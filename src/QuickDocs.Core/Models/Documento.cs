using System;
using System.Text.Json.Serialization;

namespace QuickDocs.Core.Models
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "tipoDiscriminador")]
    [JsonDerivedType(typeof(Presupuesto), "Presupuesto")]
    [JsonDerivedType(typeof(Remito), "Remito")]
    [JsonDerivedType(typeof(Recibo), "Recibo")]
    [JsonDerivedType(typeof(NotaCredito), "NotaCredito")]
    
    public class Documento
    {
        public int Id { get; set; }

        // Vinculamos el documento al Usuario que lo emite
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        
        public TipoDocumento Tipo { get; set; }
        public DateTime FechaEmision { get; set; } = DateTime.UtcNow;

        // Lógica de Talonario (Común a todos)
        public int PuntoEmision { get; set; } = 1;
        public int NumeroCorrelativo { get; set; }

        // Propiedad calculada para mostrar el formato clásico "0001-00000005"
        public string NumeroFormateado => Tipo switch
        {
            TipoDocumento.Presupuesto => $"P{PuntoEmision:D3}-{NumeroCorrelativo:D8}",
            TipoDocumento.Remito      => $"R{PuntoEmision:D3}-{NumeroCorrelativo:D8}",
            TipoDocumento.Recibo      => $"C{PuntoEmision:D3}-{NumeroCorrelativo:D8}", // 'C' de Cobro/Recibo
            TipoDocumento.NotaCredito => $"N{PuntoEmision:D3}-{NumeroCorrelativo:D8}",
            _                         => $"{PuntoEmision:D4}-{NumeroCorrelativo:D8}" // Por si acaso
        };

        // Relación flexible con el Cliente
        // El 'int?' permite que sea nulo si es una venta mostrador no registrada
        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        // Nombre del cliente (manual o copia del registrado)
        public string ClienteNombre { get; set; } = string.Empty;

        // Monto representativo del documento, según su tipo.
        // Cada subtipo la sobreescribe con el campo que corresponda.
        public virtual decimal MontoAsociado => 0m;
        // Fecha de vencimiento, si el tipo de documento la tiene (Presupuesto, NotaCredito).
        // Null para los tipos que no vencen (Remito, Recibo).
        public virtual DateTime? FechaVencimientoAsociada => null;
    }
}