using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CaseManager.Core.Models;

namespace CaseManager.Desktop.Converters;

public class DocumentKindIconConverter : IValueConverter
{
    public static readonly DocumentKindIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DocumentKind k ? k switch
        {
            DocumentKind.Document => "📄",
            DocumentKind.Photo => "🖼",
            DocumentKind.Video => "🎬",
            DocumentKind.Audio => "🎧",
            _ => "📦"
        } : "📦";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}