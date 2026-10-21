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

    public static DocumentKind FromFile(string path)
    {
        var byExt = FromFileName(path);
        if (byExt != DocumentKind.Other) return byExt;

        try
        {
            using var fs = File.OpenRead(path);
            if (fs.Length < 12) return DocumentKind.Other;
            Span<byte> h = stackalloc byte[12];
            if (fs.Read(h) < 12) return DocumentKind.Other;

            if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return DocumentKind.Photo; // PNG
            if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return DocumentKind.Photo; // JPEG
            if (h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46) return DocumentKind.Photo; // GIF
            if (h[0] == 0x25 && h[1] == 0x50 && h[2] == 0x44 && h[3] == 0x46) return DocumentKind.Document; // PDF
            if (h[0] == 0x50 && h[1] == 0x4B) return DocumentKind.Document; // ZIP (docx/xlsx)
            if (h[4] == 0x66 && h[5] == 0x74 && h[6] == 0x79 && h[7] == 0x70) return DocumentKind.Video;    // MP4 "ftyp"
        }
        catch { }

        return DocumentKind.Other;
    }
}