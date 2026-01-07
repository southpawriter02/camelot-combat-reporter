using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Services;
using CamelotCombatReporter.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

/// <summary>
/// ViewModel for the Template Selection dialog.
/// </summary>
public partial class TemplateSelectionViewModel : ObservableObject
{
    private readonly IMetaBuildTemplateService _templateService;
    private readonly CharacterClass _targetClass;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private MetaBuildTemplate? _selectedTemplate;

    [ObservableProperty]
    private Realm _selectedRealm = Realm.Unknown;

    [ObservableProperty]
    private string _selectedRole = "";

    public ObservableCollection<MetaBuildTemplate> FilteredTemplates { get; } = new();
    
    public IReadOnlyList<Realm> AvailableRealms { get; } = new[]
    {
        Realm.Unknown, // "All"
        Realm.Albion,
        Realm.Midgard,
        Realm.Hibernia
    };

    public IReadOnlyList<string> AvailableRoles { get; } = new[]
    {
        "", // "All"
        "Tank",
        "DPS",
        "Healer",
        "Support",
        "Hybrid",
        "Stealth"
    };

    public bool HasSelectedTemplate => SelectedTemplate != null;

    public string DialogTitle { get; }

    /// <summary>
    /// The result of the dialog - the selected template, or null if cancelled.
    /// </summary>
    public MetaBuildTemplate? Result { get; private set; }

    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event Action<bool>? RequestClose;

    public TemplateSelectionViewModel() : this(new MetaBuildTemplateService(), CharacterClass.Unknown)
    {
        // Design-time constructor
    }

    public TemplateSelectionViewModel(IMetaBuildTemplateService templateService, CharacterClass targetClass)
    {
        _templateService = templateService;
        _targetClass = targetClass;
        
        DialogTitle = targetClass == CharacterClass.Unknown
            ? "Select Meta Build Template"
            : $"Select Template for {targetClass}";

        RefreshTemplates();
    }

    partial void OnSearchQueryChanged(string value) => RefreshTemplates();
    partial void OnSelectedRealmChanged(Realm value) => RefreshTemplates();
    partial void OnSelectedRoleChanged(string value) => RefreshTemplates();
    partial void OnSelectedTemplateChanged(MetaBuildTemplate? value) => OnPropertyChanged(nameof(HasSelectedTemplate));

    private void RefreshTemplates()
    {
        FilteredTemplates.Clear();

        IReadOnlyList<MetaBuildTemplate> templates;

        // Start with class-specific or all templates
        if (_targetClass != CharacterClass.Unknown)
        {
            templates = _templateService.GetTemplatesForClass(_targetClass);
        }
        else if (SelectedRealm != Realm.Unknown)
        {
            templates = _templateService.GetTemplatesForRealm(SelectedRealm);
        }
        else if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            templates = _templateService.SearchTemplates(SearchQuery);
        }
        else
        {
            templates = _templateService.GetAllTemplates();
        }

        // Apply filters
        var filtered = templates.AsEnumerable();

        if (SelectedRealm != Realm.Unknown && _targetClass == CharacterClass.Unknown)
        {
            filtered = filtered.Where(t => t.Realm == SelectedRealm);
        }

        if (!string.IsNullOrWhiteSpace(SelectedRole))
        {
            filtered = filtered.Where(t => 
                t.Role.Contains(SelectedRole, StringComparison.OrdinalIgnoreCase) ||
                t.Tags.Any(tag => tag.Contains(SelectedRole, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery) && _targetClass != CharacterClass.Unknown)
        {
            var query = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(t =>
                t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var template in filtered.OrderBy(t => t.Realm).ThenBy(t => t.TargetClass.ToString()).ThenBy(t => t.Name))
        {
            FilteredTemplates.Add(template);
        }
    }

    [RelayCommand]
    private void Select()
    {
        if (SelectedTemplate != null)
        {
            Result = SelectedTemplate;
            RequestClose?.Invoke(true);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        RequestClose?.Invoke(false);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchQuery = "";
        SelectedRealm = Realm.Unknown;
        SelectedRole = "";
    }
}
