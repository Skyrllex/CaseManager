using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaseManager.Core.Models;
using CaseManager.Core.Services;

namespace CaseManager.Desktop.ViewModels;

public partial class DocumentEditViewModel : ViewModelBase
{
    private readonly CaseService _service;
    private readonly Guid _caseId;
    private readonly Guid _docId;

    public Action? OnSaved { get; set; }

    [ObservableProperty] private string _displayName = "";
    [ObservableProperty] private string _description = "";

    [ObservableProperty] private string? _selectedGroup;
    [ObservableProperty] private string _newGroupName = "";

    public ObservableCollection<string> GroupOptions { get; } = new();

    public string OriginalFileName { get; }
    public string KindLabel { get; }             

    public DocumentEditViewModel(CaseService service, Guid caseId, CaseDocument doc)
    {
        _service = service;
        _caseId = caseId;
        _docId = doc.Id;

        OriginalFileName = doc.FileName;
        _displayName = doc.DisplayName ?? doc.FileName;
        _description = doc.Description ?? "";
        _selectedGroup = doc.Group;


        KindLabel = doc.Kind switch
        {
            DocumentKind.Document => "📄 Документ",
            DocumentKind.Photo => "📷 Фото",
            DocumentKind.Video => "🎬 Видео",
            DocumentKind.Audio => "🎧 Аудио",
            _ => "📦 Другое"
        };

        RebuildGroups();
    }

    private void RebuildGroups()
    {
        GroupOptions.Clear();
        GroupOptions.Add("(без группы)");

        foreach (var g in _service.GetDocumentGroups(_caseId))
            GroupOptions.Add(g);

        if (!string.IsNullOrWhiteSpace(SelectedGroup) && !GroupOptions.Contains(SelectedGroup!))
            GroupOptions.Add(SelectedGroup!);
    }

    [RelayCommand]
    private void AddGroup()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName)) return;
        var name = NewGroupName.Trim();
        _service.AddDocumentGroup(_caseId, name);

        if (!GroupOptions.Contains(name))
            GroupOptions.Add(name);

        SelectedGroup = name;
        NewGroupName = "";
    }

    [RelayCommand]
    private void Save()
    {
        var groupValue = SelectedGroup == "(без группы)" ? null : SelectedGroup;

        _service.UpdateDocumentMeta(
            _caseId, _docId,
            displayName: DisplayName,
            description: Description,
            group: groupValue);

        OnSaved?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => OnSaved?.Invoke();

    [RelayCommand]
    private void DeleteGroup()
    {
        if (string.IsNullOrWhiteSpace(SelectedGroup)) return;
        if (SelectedGroup == "(без группы)") return;

        _service.DeleteDocumentGroup(_caseId, SelectedGroup);


        var removed = SelectedGroup;
        GroupOptions.Remove(removed);

        SelectedGroup = "(без группы)";
    }
}
