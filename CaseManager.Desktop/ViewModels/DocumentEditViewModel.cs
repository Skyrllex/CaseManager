using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaseManager.Core.Models;
using CaseManager.Core.Services;
using Avalonia;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace CaseManager.Desktop.ViewModels;

public partial class DocumentEditViewModel : ViewModelBase
{
    private readonly CaseService _service;
    private readonly Guid _caseId;
    private readonly Guid _docId;

    public Action? OnSaved { get; set; }

    [ObservableProperty] private string _displayName;
    [ObservableProperty] private string _description;
    [ObservableProperty] private NamedKind _selectedKind;
    [ObservableProperty] private string _group;

    public record NamedKind(DocumentKind Kind, string Label)
    {
        public override string ToString() => Label;
    }

    public NamedKind[] KindOptions { get; } =
    {
        new(DocumentKind.Document, "📄 Документ"),
        new(DocumentKind.Photo,    "🖼 Фото"),
        new(DocumentKind.Video,    "🎬 Видео"),
        new(DocumentKind.Audio,    "🎧 Аудио"),
        new(DocumentKind.Other,    "📦 Другое")
    };

    public string OriginalFileName { get; }

    public DocumentEditViewModel(CaseService service, Guid caseId, CaseDocument doc)
    {
        _service = service;
        _caseId = caseId;
        _docId = doc.Id;

        OriginalFileName = doc.FileName;
        _displayName = doc.DisplayName ?? doc.FileName;
        _description = doc.Description ?? "";
        _group = doc.Group ?? "";
        _selectedKind = KindOptions.FirstOrDefault(k => k.Kind == doc.Kind) ?? KindOptions[0];
    }

    [RelayCommand]
    private void Save()
    {
        _service.UpdateDocumentMeta(
            _caseId, _docId,
            displayName: DisplayName,
            description: Description,
            kind: SelectedKind.Kind,
            group: Group);

        OnSaved?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => OnSaved?.Invoke();
}