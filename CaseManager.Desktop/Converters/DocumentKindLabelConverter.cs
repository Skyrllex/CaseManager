using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CaseManager.Core.Models;

namespace CaseManager.Desktop.Converters;

public class DocumentKindLabelConverter : IValueConverter
{
    public static readonly DocumentKindLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DocumentKind k ? k switch
        {
            DocumentKind.Document => "Документ",
            DocumentKind.Photo => "Фото",
            DocumentKind.Video => "Видео",
            DocumentKind.Audio => "Аудио",
            _ => "Другое"
        } : "Другое";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}