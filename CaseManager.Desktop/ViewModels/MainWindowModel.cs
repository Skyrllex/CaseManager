using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaseManager.Core.Models;
using CaseManager.Core.Services;
using CaseManager.Desktop.Views;
using System.Linq;
using System;
using Avalonia.Controls;
using System.Collections.Generic;

namespace CaseManager.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly CaseService _service;

    public ObservableCollection<Case> Cases { get; } = new();

    [ObservableProperty] private Case? _selectedCase;

    public MainWindowViewModel(CaseService service)
    {
        _service = service;
        Refresh();
    }

    public void Refresh()
    {
        var selectedId = SelectedCase?.Id;
        Cases.Clear();
        foreach (var c in _service.ListAll().OrderByDescending(x => x.UpdatedAt))
            Cases.Add(c);
        if (selectedId != null)
            SelectedCase = Cases.FirstOrDefault(c => c.Id == selectedId);
    }

    [RelayCommand]
    private void CreateCase()
    {
        var editorVm = new CaseEditorViewModel(_service, null);
        var win = new CaseEditorWindow { DataContext = editorVm };
        editorVm.OnSaved = () => { win.Close(); Refresh(); };
        win.ShowDialog(App.MainWindow!);
    }

    [RelayCommand]
    private void EditCase()
    {
        if (SelectedCase == null) return;
        var editorVm = new CaseEditorViewModel(_service, SelectedCase.Id);
        var win = new CaseEditorWindow { DataContext = editorVm };
        editorVm.OnSaved = () => { win.Close(); Refresh(); };
        win.ShowDialog(App.MainWindow!);
    }

    [RelayCommand]
    private void DeleteCase()
    {
        if (SelectedCase == null) return;
        try { _service.Delete(SelectedCase.Id); }
        catch { /* файл занят — пока просто игнорируем, позже добавим диалог */ }
        Refresh();
    }

    [RelayCommand]
    private async Task AddDocument()
    {
        if (SelectedCase == null) return;

        var storage = App.MainWindow?.StorageProvider;
        if (storage == null) return;

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите материалы дела",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Все файлы") { Patterns = new[] { "*.*" } },
                new FilePickerFileType("Документы")
                {
                    Patterns = new[] { "*.pdf", "*.doc", "*.docx", "*.txt", "*.rtf", "*.xls", "*.xlsx" }
                },
                new FilePickerFileType("Изображения")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp" }
                },
                new FilePickerFileType("Видео / аудио")
                {
                    Patterns = new[] { "*.mp4", "*.mov", "*.avi", "*.mkv", "*.mp3", "*.wav", "*.m4a" }
                }
            }
        });
        var caseId = SelectedCase.Id;
        foreach (var f in files)
        {
            var path = f.TryGetLocalPath();
            if (path != null)
            {
                try { _service.AddDocument(caseId, path); }
                catch { /* пропускаем битый файл */ }
            }
        }

        Refresh();
        SelectedCase = Cases.FirstOrDefault(c => c.Id == caseId);
    }

    public void OpenDocument(CaseDocument? doc, Window? owner = null)
    {
        if (doc == null || SelectedCase == null) return;

        var path = _service.GetDocumentFullPath(SelectedCase.Id, doc);
        if (path == null)
        {
            System.Diagnostics.Debug.WriteLine($"Файл не найден: {doc.FileName}");
            return;
        }

        var viewer = new DocumentViewerWindow(path);
        var parent = owner ?? App.MainWindow;
        if (parent != null)
            viewer.ShowDialog(parent);
        else
            viewer.Show();
    }
    // Выбранный документ
    [ObservableProperty] private CaseDocument? _selectedDocument;

    // Фильтр документа: null = все
    [ObservableProperty] private DocumentKind? _documentFilter;

    public DocumentKind?[] DocumentFilterOptions { get; } =
        new DocumentKind?[] { null, DocumentKind.Document, DocumentKind.Photo,
                          DocumentKind.Video, DocumentKind.Audio, DocumentKind.Other };

    // Отображаемый (фильтрованный) список документов
    public IEnumerable<CaseDocument> FilteredDocuments =>
        SelectedCase?.Documents
            .Where(d => DocumentFilter == null || d.Kind == DocumentFilter)
            .OrderByDescending(d => d.UploadedAt) ?? Enumerable.Empty<CaseDocument>();

    partial void OnDocumentFilterChanged(DocumentKind? value)
        => OnPropertyChanged(nameof(FilteredDocuments));

    partial void OnSelectedCaseChanged(Case? value)
    {
        SelectedDocument = null;
        OnPropertyChanged(nameof(FilteredDocuments));
    }

    //[RelayCommand]
    public void DeleteDocument(CaseDocument? doc)
    {
        if (doc == null || SelectedCase == null) return;
        try { _service.DeleteDocument(SelectedCase.Id, doc.Id); }
        catch { }
        Refresh();
        SelectedCase = Cases.FirstOrDefault(c => c.Id == SelectedCase!.Id);
    }

    // Редактирования  документа
    //[RelayCommand]
    public void EditDocument(CaseDocument? doc)
    {
        if (doc == null || SelectedCase == null) return;

        var caseId = SelectedCase.Id;
        var docId = doc.Id;

        var dialogVm = new DocumentEditViewModel(_service, caseId, doc);
        var win = new DocumentEditWindow { DataContext = dialogVm };
        dialogVm.OnSaved = () =>
        {
            win.Close();
            Refresh();
            SelectedCase = Cases.FirstOrDefault(c => c.Id == caseId);
        };
        win.ShowDialog(App.MainWindow!);
    }
}