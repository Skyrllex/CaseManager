using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaseManager.Core.Models;
using CaseManager.Core.Services;
using System.Linq;
using System;

namespace CaseManager.Desktop.ViewModels;

public partial class CaseEditorViewModel : ViewModelBase
{
    private readonly CaseService _service;
    private readonly Guid? _editId;

    public Action? OnSaved { get; set; }

    // Пара «английский ключ + русская подпись» для ComboBox
    public record NamedOption(string Key, string Label)
    {
        public override string ToString() => Label;
    }

    public NamedOption[] TypeOptions { get; } =
    {
        new(nameof(CaseType.Civil),          "Гражданское"),
        new(nameof(CaseType.Criminal),       "Уголовное"),
        new(nameof(CaseType.Administrative), "Административное"),
        new(nameof(CaseType.Arbitration),    "Арбитражное"),
        new(nameof(CaseType.Other),          "Иное")
    };

    public NamedOption[] StageOptions { get; } =
    {
        new(nameof(CaseStage.Started),    "Старт"),
        new(nameof(CaseStage.InProgress), "Идёт процесс"),
        new(nameof(CaseStage.Appeal),     "Апелляция"),
        new(nameof(CaseStage.Cassation),  "Кассация"),
        new(nameof(CaseStage.Closed),     "Завершено")
    };

    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _shortDescription = "";
    [ObservableProperty] private string _goal = "";
    [ObservableProperty] private NamedOption? _selectedType;
    [ObservableProperty] private NamedOption? _selectedStage;

    public ObservableCollection<Party> Parties { get; } = new();

    [ObservableProperty] private Party? _selectedParty;

    [ObservableProperty] private string _newPartyName = "";
    [ObservableProperty] private string _newPartyRole = "";
    [ObservableProperty] private string _newPartyContacts = "";
    [ObservableProperty] private string _newPartyAddress = "";
    [ObservableProperty] private string _newPartyDescription = "";

    public string WindowTitle => _editId == null ? "Новое дело" : "Редактирование дела";

    public CaseEditorViewModel(CaseService service, Guid? editId)
    {
        _service = service;
        _editId = editId;

        // дефолты для нового дела
        SelectedType = TypeOptions[0];
        SelectedStage = StageOptions[0];

        if (editId != null)
        {
            var c = service.Get(editId.Value);
            if (c != null)
            {
                Title = c.Title;
                ShortDescription = c.ShortDescription;
                Goal = c.Goal;

                SelectedType = TypeOptions.FirstOrDefault(o => o.Key == c.Type.ToString())
                                ?? TypeOptions[0];
                SelectedStage = StageOptions.FirstOrDefault(o => o.Key == c.Stage.ToString())
                                ?? StageOptions[0];

                // Копируем участников, а не шарим ссылки.
                // Иначе редактирование в окне сразу меняет дело в памяти,
                // и «Отмена» не откатит изменения.
                foreach (var p in c.Parties)
                {
                    Parties.Add(new Party
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Role = p.Role,
                        Contacts = p.Contacts,
                        Address = p.Address,
                        Description = p.Description
                    });
                }
            }
        }
    }

    [RelayCommand]
    private void AddParty()
    {
        if (string.IsNullOrWhiteSpace(NewPartyName)) return;

        Parties.Add(new Party
        {
            Name = NewPartyName.Trim(),
            Role = NewPartyRole.Trim(),
            Contacts = string.IsNullOrWhiteSpace(NewPartyContacts) ? null : NewPartyContacts.Trim(),
            Address = string.IsNullOrWhiteSpace(NewPartyAddress) ? null : NewPartyAddress.Trim(),
            Description = string.IsNullOrWhiteSpace(NewPartyDescription) ? null : NewPartyDescription.Trim()
        });
        NewPartyName = "";
        NewPartyRole = "";
        NewPartyContacts = "";
        NewPartyAddress = "";
        NewPartyDescription = "";
    }

    [RelayCommand]
    private void RemoveParty()
    {
        if (SelectedParty == null) return;
        Parties.Remove(SelectedParty);
        SelectedParty = null;
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Title)) return;

        var type = Enum.Parse<CaseType>(SelectedType!.Key);
        var stage = Enum.Parse<CaseStage>(SelectedStage!.Key);

        if (_editId == null)
        {
            var c = _service.CreateCase(Title, ShortDescription, Goal, type, Parties);
            c.Stage = stage;
            _service.Save(c);
        }
        else
        {
            var c = _service.Get(_editId.Value)!;
            c.Title = Title;
            c.ShortDescription = ShortDescription;
            c.Goal = Goal;
            c.Type = type;
            c.Stage = stage;
            c.Parties = Parties.ToList();
            _service.Save(c);
        }

        OnSaved?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => OnSaved?.Invoke();
}