using System;
using System.Collections.Generic;
using System.IO;

namespace CaseManager.Core.Models;

public static class DocumentKindHelper
{
    private static readonly HashSet<string> DocExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".rtf",
        ".odt", ".ods", ".md", ".json", ".xml", ".csv", ".html", ".htm"
    };

    private static readonly HashSet<string> PhotoExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tif", ".tiff", ".heic"
    };

    private static readonly HashSet<string> VideoExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".flv", ".m4v", ".mpg", ".mpeg"
    };

    private static readonly HashSet<string> AudioExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac", ".wma", ".opus"
    };

    public static DocumentKind FromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return DocumentKind.Other;

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext)) return DocumentKind.Other;

        if (PhotoExt.Contains(ext)) return DocumentKind.Photo;
        if (VideoExt.Contains(ext)) return DocumentKind.Video;
        if (AudioExt.Contains(ext)) return DocumentKind.Audio;
        if (DocExt.Contains(ext)) return DocumentKind.Document;

        return DocumentKind.Other;
    }
}