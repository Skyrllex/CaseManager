using CaseManager.Core.Models;

public record FilterOption(DocumentKind? Kind, string Label)
{
    public override string ToString() => Label;
}

public static class DocumentFilters
{
    public static readonly FilterOption[] All =
    {
        new(null,                  "Все материалы"),
        new(DocumentKind.Document, "📄 Документы"),
        new(DocumentKind.Photo,    "🖼 Фото"),
        new(DocumentKind.Video,    "🎬 Видео"),
        new(DocumentKind.Audio,    "🎧 Аудио"),
        new(DocumentKind.Other,    "📦 Другое")
    };
}