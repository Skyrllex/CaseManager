using System.Collections.Generic;
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CaseManager.Core.Models;

namespace CaseManager.Desktop.Converters;

public class CaseTypeConverter : IValueConverter
{
    public static readonly CaseTypeConverter Instance = new();

    private static readonly Dictionary<CaseType, string> Ru = new()
    {
        [CaseType.Civil] = "Гражданское",
        [CaseType.Criminal] = "Уголовное",
        [CaseType.Administrative] = "Административное",
        [CaseType.Arbitration] = "Арбитражное",
        [CaseType.Other] = "Иное"
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CaseType t && Ru.TryGetValue(t, out var ru)) return ru;
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}