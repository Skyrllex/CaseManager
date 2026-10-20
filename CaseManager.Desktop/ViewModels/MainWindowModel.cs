using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaseManager.Core.Models;
using CaseManager.Core.Services;
using CaseManager.Desktop.Views;

namespace CaseManager.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly CaseService _service;


    public ObservableCollection<Case> Cases { get; } = new();

    [ObservableProperty] private Case? _selectedCase;


    [ObservableProperty] private CaseDocument? _selectedDocument;


    public FilterOption[] DocumentFilterOptions => DocumentFilters.All;

    [ObservableProperty] private FilterOption _selectedDocumentFilter;


    public ObservableCollection<CaseDocument> FilteredDocuments { get; } = new();



    public MainWindowViewModel(CaseService service)
    {
        _service = service;
        SelectedDocumentFilter = DocumentFilters.All[0]; // Все 
        Refresh();
    }

    //Обновление

    partial void OnSelectedDocumentFilterChanged(FilterOption value)
        => RebuildFilteredDocuments();

    partial void OnSelectedCaseChanged(Case? value)
    {
        SelectedDocument = null;
        RebuildFilteredDocuments();
    }

    private void RebuildFilteredDocuments()
    {
        FilteredDocuments.Clear();
        if (SelectedCase == null) return;

        var kind = SelectedDocumentFilter?.Kind;
        var docs = SelectedCase.Documents
            .Where(d => kind == null || d.Kind == kind)
            .OrderByDescending(d => d.UploadedAt);

        foreach (var d in docs)
            FilteredDocuments.Add(d);
    }

    // Список дел

    public void Refresh()
    {
        var selectedId = SelectedCase?.Id;
        Cases.Clear();
        foreach (var c in _service.ListAll().OrderByDescending(x => x.UpdatedAt))
            Cases.Add(c);
        if (selectedId != null)
            SelectedCase = Cases.FirstOrDefault(c => c.Id == selectedId);

        RebuildFilteredDocuments();
    }

    // дела

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
        catch { }
        Refresh();
    }

    //Добавить документ

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
            FileTypeFilter = new[]{
                new FilePickerFileType("Все файлы") { Patterns = new[] { "*.*" } },
                new FilePickerFileType("Документы")
                {
                    Patterns = new[] { "*.pdf", "*.doc", "*.docx", "*.txt", "*.rtf",
                                       "*.xls", "*.xlsx" }
                },
                new FilePickerFileType("Изображения")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp" }
                },
                new FilePickerFileType("Видео / аудио")
                {
                    Patterns = new[] { "*.mp4", "*.mov", "*.avi", "*.mkv",
                                       "*.mp3", "*.wav", "*.m4a" }
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
                catch { }
            }
        }

        Refresh();
        SelectedCase = Cases.FirstOrDefault(c => c.Id == caseId);
    }

    // открыть

    public void OpenDocument(CaseDocument? doc, Window? owner = null)
    {
        if (doc == null || SelectedCase == null) return;

        try
        {
            var path = _service.GetDocumentFullPath(SelectedCase.Id, doc);
            if (path == null)
            {
                System.Diagnostics.Debug.WriteLine($"Файл не найден: {doc.FileName}");
                return;
            }

            var viewer = new DocumentViewerWindow(path);
            var parent = owner ?? App.MainWindow;
            if (parent != null) viewer.ShowDialog(parent);
            else viewer.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenDocument failed: {ex}");
        }
    }

    //  удалить 

    public void DeleteDocument(CaseDocument? doc)
    {
        if (doc == null || SelectedCase == null) return;

        var caseId = SelectedCase.Id;
        try { _service.DeleteDocument(caseId, doc.Id); }
        catch { }

        Refresh();
        SelectedCase = Cases.FirstOrDefault(c => c.Id == caseId);
    }

    //  редактировать

    public void EditDocument(CaseDocument? doc)
    {
        if (doc == null || SelectedCase == null) return;

        var caseId = SelectedCase.Id;

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