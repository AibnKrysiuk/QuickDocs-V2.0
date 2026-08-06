using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using QuickDocs.Core.Models;

namespace QuickDocs.UI.Converters
{
    public class MontoColorConverter : IValueConverter
    {
        public static readonly MontoColorConverter Instance = new();

        private static readonly IBrush Verde = new SolidColorBrush(Color.Parse("#10B981"));
        private static readonly IBrush Naranja = new SolidColorBrush(Color.Parse("#F59E0B"));
        private static readonly IBrush Neutro = new SolidColorBrush(Color.Parse("#0F172A"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Documento doc) return Neutro;

            return doc.Tipo switch
            {
                TipoDocumento.Recibo => Verde,
                TipoDocumento.NotaCredito => Naranja,
                _ => Neutro
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}