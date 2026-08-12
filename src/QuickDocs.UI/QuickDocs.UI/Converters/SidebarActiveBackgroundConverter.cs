using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace QuickDocs.UI.Converters
{
    public class SidebarActiveBackgroundConverter : IValueConverter
    {
        public static readonly SidebarActiveBackgroundConverter Instance = new();

        private static readonly IBrush Activo = new SolidColorBrush(Color.Parse("#334155"));
        private static readonly IBrush Inactivo = Brushes.Transparent;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string actual && parameter is string clave &&
                string.Equals(actual, clave, StringComparison.OrdinalIgnoreCase))
                return Activo;

            return Inactivo;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}