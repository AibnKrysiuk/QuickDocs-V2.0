using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace QuickDocs.UI.Converters
{
    public class SidebarActiveForegroundConverter : IValueConverter
    {
        public static readonly SidebarActiveForegroundConverter Instance = new();

        private static readonly IBrush Activo = new SolidColorBrush(Color.Parse("#F8FAFC"));
        private static readonly IBrush Inactivo = new SolidColorBrush(Color.Parse("#E2E8F0"));

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