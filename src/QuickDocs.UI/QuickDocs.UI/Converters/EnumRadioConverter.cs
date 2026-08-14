using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace QuickDocs.UI.Converters
{
    public class EnumRadioConverter : IValueConverter
    {
        public static readonly EnumRadioConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value?.ToString() == parameter?.ToString();

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && b) ? Enum.Parse(typeof(QuickDocs.UI.ViewModels.TipoDescuentoUI), parameter!.ToString()!) : BindingOperations.DoNothing;
    }
}