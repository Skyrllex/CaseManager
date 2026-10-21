using CaseManager.Core.Models;

namespace CaseManager.Desktop.ViewModels;

public record DocumentFilter(DocumentKind? Kind, string Label)
{
    public override string ToString() => Label;
}

public static class DocumentFilters
{
    public static readonly DocumentFilter[] All =
    {
        new(null,                  "Все материалы"),
        new(DocumentKind.Document, "📄 Документы"),
        new(DocumentKind.Photo,    "📷 Фото"),
        new(DocumentKind.Video,    "🎬 Видео"),
        new(DocumentKind.Audio,    "🎧 Аудио"),
        new(DocumentKind.Other,    "📦 Другое")
    };
}