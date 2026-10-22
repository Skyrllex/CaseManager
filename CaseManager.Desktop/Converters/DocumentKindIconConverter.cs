using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CaseManager.Core.Models;

namespace CaseManager.Desktop.Converters;

public class DocumentKindIconConverter : IValueConverter
{
    public static readonly DocumentKindIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DocumentKind k) return "❓";
        return k switch
        {
            DocumentKind.Document => "📄",
            DocumentKind.Photo => "📷",
            DocumentKind.Video => "🎬",
            DocumentKind.Audio => "🎧",
            DocumentKind.Other => "📦",
            _ => "❓"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}