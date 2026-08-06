using System;
using System.Globalization;
using Avalonia.Data.Converters;
using QuickDocs.Core.Models;

namespace QuickDocs.UI.Converters
{
    public class MontoTextoConverter : IValueConverter
    {
        public static readonly MontoTextoConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Documento doc) return string.Empty;

            string montoFormateado = doc.MontoAsociado.ToString("C", culture);

            return doc.Tipo switch
            {
                TipoDocumento.Recibo => $"+ {montoFormateado}",
                TipoDocumento.NotaCredito => $"- {montoFormateado}",
                _ => montoFormateado
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}