using System.Collections.Generic;
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CaseManager.Core.Models;

namespace CaseManager.Desktop.Converters;

public class CaseStageConverter : IValueConverter
{
    public static readonly CaseStageConverter Instance = new();

    private static readonly Dictionary<CaseStage, string> Ru = new()
    {
        [CaseStage.Started] = "Старт",
        [CaseStage.InProgress] = "Идёт процесс",
        [CaseStage.Appeal] = "Апелляция",
        [CaseStage.Cassation] = "Кассация",
        [CaseStage.Closed] = "Завершено"
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CaseStage s && Ru.TryGetValue(s, out var ru)) return ru;
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}