using System.Collections.Generic;
using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CaseManager.Desktop.Converters;

public class PartyRoleConverter : IValueConverter
{
    public static readonly PartyRoleConverter Instance = new();

    private static readonly Dictionary<string, string> Ru = new(StringComparer.OrdinalIgnoreCase)
    {
        ["plaintiff"] = "Истец",
        ["defendant"] = "Ответчик",
        ["thirdparty"] = "Третье лицо",
        ["judge"] = "Судья",
        ["lawyer"] = "Представитель",
        ["prosecutor"] = "Прокурор",
        ["witness"] = "Свидетель"
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && Ru.TryGetValue(s, out var ru)) return ru;
        return value?.ToString();  // если не нашли — показываем как есть
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            foreach (var kv in Ru)
                if (kv.Value.Equals(s, StringComparison.OrdinalIgnoreCase)) return kv.Key;
        }
        return value;
    }
}