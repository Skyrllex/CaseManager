using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CaseManager.Desktop.Converters;

public class FileSizeConverter : IValueConverter
{
    public static readonly FileSizeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return "—";
        if (value is not long bytes) return "?";
        if (bytes <= 0) return "0 Б";

        string[] units = { "Б", "КБ", "МБ", "ГБ" };
        double size = bytes;
        int i = 0;
        while (size >= 1024 && i < units.Length - 1) { size /= 1024; i++; }
        return $"{size:0.#} {units[i]}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}