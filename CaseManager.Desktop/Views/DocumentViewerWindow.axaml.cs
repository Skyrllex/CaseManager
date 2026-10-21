using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace CaseManager.Desktop.Views;

public partial class DocumentViewerWindow : Window
{
    private static readonly HashSet<string> TextExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".json", ".csv", ".log", ".md", ".xml",
        ".html", ".htm", ".css", ".js", ".cs", ".sql",
        ".yml", ".yaml", ".ini", ".config"
    };

    private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp"
    };

    private static readonly HashSet<string> ExternalSafeExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx",
        ".rtf", ".odt", ".ods",
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv",   
        ".mp3", ".wav", ".ogg", ".flac", ".m4a"            
    };

    private string _fullPath = "";

    public DocumentViewerWindow()
    {
        InitializeComponent();
    }

    public DocumentViewerWindow(string filePath) : this()
    {
        _fullPath = filePath;

        try
        {
            FileNameText.Text = Path.GetFileName(filePath);
            OpenExternalButton.Click += OnOpenExternal;
            LoadPreview();
        }
        catch (Exception ex)
        {
            // Абсолютный fallback — что бы ни случилось, окно живо
            ShowFallback("Не удалось загрузить предпросмотр: " + ex.Message);
        }
    }

    private void LoadPreview()
    {
        if (string.IsNullOrWhiteSpace(_fullPath) || !File.Exists(_fullPath))
        {
            ShowFallback("Файл не найден.");
            OpenExternalButton.IsEnabled = false;
            return;
        }

        var ext = Path.GetExtension(_fullPath);

        // 1) Текст — только если расширение текстовое И файл не гигантский
        if (TextExts.Contains(ext))
        {
            try
            {
                var fi = new FileInfo(_fullPath);
                if (fi.Length > 5 * 1024 * 1024) // >5 МБ текста — не грузим
                {
                    ShowFallback("Файл слишком большой для предпросмотра (более 5 МБ).");
                    return;
                }

                var text = File.ReadAllText(_fullPath, Encoding.UTF8);
                const int max = 1_000_000;
                if (text.Length > max)
                    text = text[..max] + "\n\n… (файл обрезан)";

                TextContent.Text = text;
                TextScroll.IsVisible = true;
                return;
            }
            catch
            {
                // упадём в fallback
            }
        }

        // 2) Картинка — только если расширение картинки И сигнатура файла совпадает
        if (ImageExts.Contains(ext) && IsRealImage(_fullPath))
        {
            try
            {
                ImageContent.Source = new Bitmap(_fullPath);
                ImageScroll.IsVisible = true;
                return;
            }
            catch
            {
                // упадём в fallback
            }
        }

        // 3) Всё остальное (видео, pdf, dll, exe, архив) — fallback
        if (ExternalSafeExts.Contains(ext))
            ShowFallback("Встроенный просмотр недоступен. Откройте файл во внешнем приложении.");
        else
        {
            ShowFallback("Формат не поддерживается для предпросмотра.");
            OpenExternalButton.Content = "↗️ Открыть всё равно";
        }
    }

    private void ShowFallback(string message)
    {
        FallbackPanel.IsVisible = true;
        TextScroll.IsVisible = false;
        ImageScroll.IsVisible = false;
        // Пытаемся обновить текст в FallbackPanel, если он там есть.
        // (На случай, если XAML был изменён — просто молча игнорируем.)
        try
        {
            foreach (var child in ((StackPanel)FallbackPanel).Children)
            {
                if (child is TextBlock tb && tb.FontSize >= 13 && tb.FontSize <= 15)
                {
                    tb.Text = message;
                    break;
                }
            }
        }
        catch { /* ничего страшного */ }
    }

    private void OnOpenExternal(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(_fullPath))
            {
                FileNameText.Text = "Файл не найден";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _fullPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            FileNameText.Text = $"{Path.GetFileName(_fullPath)}  (не удалось открыть: {ex.Message})";
        }
    }

    /// Проверка, что файл реально картинка — по magic bytes, а не по расширению.
    /// Защищает от переименованных .mp4 → .png и битых файлов.

    private static bool IsRealImage(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            if (fs.Length < 12) return false;

            Span<byte> header = stackalloc byte[12];
            var read = fs.Read(header);
            if (read < 12) return false;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                return true;

            // JPEG: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return true;

            // GIF: 47 49 46 38 ("GIF8")
            if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
                return true;

            // BMP: 42 4D ("BM")
            if (header[0] == 0x42 && header[1] == 0x4D)
                return true;

            // WEBP: RIFF....WEBP
            if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}