using System;
using System.Collections.Generic;
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

    //  ДЕЛА

    public ObservableCollection<Case> Cases { get; } = new();
    public ObservableCollection<Case> FilteredCases { get; } = new();

    [ObservableProperty] private Case? _selectedCase;

    // Фильтр дел: тип / стадия
    public record CaseFilterOption(CaseType? Type, CaseStage? Stage, string Label)
    {
        public override string ToString() => Label;
    }

    public CaseFilterOption[] CaseFilterOptions { get; } =
    {
        new(null, null, "Все дела"),
        new(null, CaseStage.Started, "🟡 Старт"),
        new(null, CaseStage.InProgress, "🟢 Идёт процесс"),
        new(null, CaseStage.Appeal, "🟠 Апелляция"),
        new(null, CaseStage.Cassation, "🟣 Кассация"),
        new(null, CaseStage.Closed, "⚪️ Завершено"),
        new(CaseType.Civil, null, "📘 Гражданские"),
        new(CaseType.Criminal, null, "📕 Уголовные"),
        new(CaseType.Administrative, null, "📙 Административные"),
        new(CaseType.Arbitration, null, "📗 Арбитражные"),
        new(CaseType.Other, null, "📒 Иное")  
    };

    [ObservableProperty] private CaseFilterOption _selectedCaseFilter = null!;

    // ДОКУМЕНТЫ 

    [ObservableProperty] private CaseDocument? _selectedDocument;

    // тип документов
    public DocumentFilter[] DocumentFilterOptions => DocumentFilters.All;
    [ObservableProperty] private DocumentFilter _selectedDocumentFilter = null!;

    //  группы документов
    public ObservableCollection<string> DocumentGroups { get; } = new();
    [ObservableProperty] private string _selectedDocumentGroup = "Все группы";

    // Отфильтрованный список документов
    public ObservableCollection<CaseDocument> FilteredDocuments { get; } = new();

    //КОНСТРУКТОР 

    public MainWindowViewModel(CaseService service)
    {
        _service = service;

        SelectedCaseFilter = CaseFilterOptions[0];         // «Все дела»
        SelectedDocumentFilter = DocumentFilters.All[0];   // «Все материалы»

        Refresh();
    }



    partial void OnSelectedCaseFilterChanged(CaseFilterOption value)
        => RebuildFilteredCases();

    partial void OnSelectedCaseChanged(Case? value)
    {
        SelectedDocument = null;
        RebuildDocumentGroups();
        RebuildFilteredDocuments();
    }

    partial void OnSelectedDocumentFilterChanged(DocumentFilter value)
        => RebuildFilteredDocuments();

    partial void OnSelectedDocumentGroupChanged(string value)
        => RebuildFilteredDocuments();


    public void Refresh()
    {
        var selectedId = SelectedCase?.Id;

        Cases.Clear();
        foreach (var c in _service.ListAll().OrderByDescending(x => x.UpdatedAt))
            Cases.Add(c);

        if (selectedId != null)
            SelectedCase = Cases.FirstOrDefault(c => c.Id == selectedId);

        RebuildFilteredCases();
        RebuildDocumentGroups();
        RebuildFilteredDocuments();
    }

    //ФИЛЬТР ДЕЛ 

    private void RebuildFilteredCases()
    {
        FilteredCases.Clear();
        if (SelectedCaseFilter == null) return;

        var t = SelectedCaseFilter.Type;
        var s = SelectedCaseFilter.Stage;
        foreach (var c in Cases)
        {
            if (t != null && c.Type != t) continue;
            if (s != null && c.Stage != s) continue;
            FilteredCases.Add(c);
        }
    }

    // ГРУППЫ ДОКУМЕНТОВ

    private void RebuildDocumentGroups()
    {
        DocumentGroups.Clear();
        DocumentGroups.Add("Все группы");

        if (SelectedCase == null) return;

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1) справочник дела
        foreach (var g in _service.GetDocumentGroups(SelectedCase.Id))
            set.Add(g);

        // 2) страховка: группы, реально используемые в документах
        foreach (var d in SelectedCase.Documents)
            if (!string.IsNullOrWhiteSpace(d.Group))
                set.Add(d.Group!);

        foreach (var g in set.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase))
            DocumentGroups.Add(g);

        DocumentGroups.Add("Без группы");

        // Сброс выбора, если выбранная группа исчезла
        if (!DocumentGroups.Contains(SelectedDocumentGroup))
            SelectedDocumentGroup = "Все группы";
    }

    // Документы

    private void RebuildFilteredDocuments()
    {
        FilteredDocuments.Clear();
        if (SelectedCase == null) return;

        var kind = SelectedDocumentFilter?.Kind;
        var group = SelectedDocumentGroup;

        IEnumerable<CaseDocument> docs = SelectedCase.Documents;

        if (kind != null)
            docs = docs.Where(d => d.Kind == kind);

        if (!string.IsNullOrEmpty(group) && group != "Все группы")
        {
            if (group == "Без группы")
                docs = docs.Where(d => string.IsNullOrWhiteSpace(d.Group));
            else
                docs = docs.Where(d => d.Group == group);
        }

        foreach (var d in docs.OrderByDescending(d => d.UploadedAt))
            FilteredDocuments.Add(d);
    }

    // ДЕЛА

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

    //ДОКУМЕНТ 

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
                    Patterns = new[] { "*.pdf", "*.doc", "*.docx", "*.txt", "*.rtf",
                                       "*.xls", "*.xlsx" }
                },
                new FilePickerFileType("Изображения")
                {Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp" }
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



    public void DeleteDocument(CaseDocument? doc)
    {
        if (doc == null || SelectedCase == null) return;

        var caseId = SelectedCase.Id;
        try { _service.DeleteDocument(caseId, doc.Id); }
        catch { }

        Refresh();
        SelectedCase = Cases.FirstOrDefault(c => c.Id == caseId);
    }

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