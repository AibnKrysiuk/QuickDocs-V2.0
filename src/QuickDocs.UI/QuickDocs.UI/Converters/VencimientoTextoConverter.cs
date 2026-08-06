using System;
using System.Globalization;
using Avalonia.Data.Converters;
using QuickDocs.Core.Models;

namespace QuickDocs.UI.Converters
{
    public class VencimientoTextoConverter : IValueConverter
    {
        public static readonly VencimientoTextoConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Documento doc || doc.FechaVencimientoAsociada is not DateTime vencimiento)
                return string.Empty;

            var restante = vencimiento - DateTime.UtcNow;

            if (restante.TotalHours <= 0) return "Vencido";
            if (restante.TotalHours <= 12) return "Vence hoy";
            return "Vence mañana";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}