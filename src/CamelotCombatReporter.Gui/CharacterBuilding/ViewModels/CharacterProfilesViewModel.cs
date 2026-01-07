using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Services;
using CamelotCombatReporter.Core.CrossRealm;
using CamelotCombatReporter.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

/// <summary>
/// ViewModel for the character profiles view.
/// </summary>
public partial class CharacterProfilesViewModel : ObservableObject
{
    private readonly ICharacterProfileService _profileService;
    private readonly ICrossRealmStatisticsService _sessionService;

    [ObservableProperty]
    private ObservableCollection<CharacterProfile> _profiles = [];

    [ObservableProperty]
    private CharacterProfile? _selectedProfile;

    [ObservableProperty]
    private ObservableCollection<CombatSessionSummary> _attachedSessions = [];

    [ObservableProperty]
    private BuildPerformanceMetrics? _performanceMetrics;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    // Current session available for attachment (set by MainWindow after parsing)
    private ExtendedCombatStatistics? _currentSession;

    public CharacterProfilesViewModel() 
        : this(new CharacterProfileService(), new CrossRealmStatisticsService())
    {
    }

    public CharacterProfilesViewModel(
        ICharacterProfileService profileService,
        ICrossRealmStatisticsService sessionService)
    {
        _profileService = profileService;
        _sessionService = sessionService;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Computed Properties
    // ─────────────────────────────────────────────────────────────────────────

    public bool HasProfiles => Profiles.Count > 0;
    public bool HasSelectedProfile => SelectedProfile != null;
    public bool HasActiveBuild => SelectedProfile?.ActiveBuild != null;
    public bool HasAttachedSessions => AttachedSessions.Count > 0;
    public bool HasPerformanceMetrics => PerformanceMetrics != null;
    public bool HasCurrentSession => _currentSession != null;

    // ─────────────────────────────────────────────────────────────────────────
    // Grouped Profiles (for future TreeView)
    // ─────────────────────────────────────────────────────────────────────────

    public IEnumerable<RealmProfileGroup> GroupedProfiles => 
        Profiles
            .GroupBy(p => p.Realm)
            .Select(g => new RealmProfileGroup(g.Key.ToString(), g.ToList()))
            .OrderBy(g => g.RealmName);

    // ─────────────────────────────────────────────────────────────────────────
    // Initialization
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the view model and loads profiles.
    /// </summary>
    public async Task InitializeAsync()
    {
        await RefreshProfilesAsync();
    }

    partial void OnSelectedProfileChanged(CharacterProfile? value)
    {
        if (value != null)
        {
            _ = LoadProfileDetailsAsync(value.Id);
        }
        else
        {
            AttachedSessions.Clear();
            PerformanceMetrics = null;
        }
        
        OnPropertyChanged(nameof(HasSelectedProfile));
        OnPropertyChanged(nameof(HasActiveBuild));
    }

    partial void OnFilterTextChanged(string value)
    {
        _ = RefreshProfilesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task CreateProfile()
    {
        var dialog = new Views.ProfileEditorDialog();
        var mainWindow = GetMainWindow();
        
        if (mainWindow != null)
        {
            var result = await dialog.ShowDialog<CharacterProfile?>(mainWindow);
            if (result != null)
            {
                try
                {
                    var created = await _profileService.CreateProfileAsync(result);
                    await RefreshProfilesAsync();
                    SelectedProfile = Profiles.FirstOrDefault(p => p.Id == created.Id);
                    StatusMessage = $"Created profile: {created.Name}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error creating profile: {ex.Message}";
                }
            }
        }
    }

    [RelayCommand]
    private async Task EditProfile()
    {
        if (SelectedProfile == null) return;

        var dialog = new Views.ProfileEditorDialog(SelectedProfile);
        var mainWindow = GetMainWindow();
        
        if (mainWindow != null)
        {
            var result = await dialog.ShowDialog<CharacterProfile?>(mainWindow);
            if (result != null)
            {
                try
                {
                    await _profileService.UpdateProfileAsync(result);
                    await RefreshProfilesAsync();
                    SelectedProfile = Profiles.FirstOrDefault(p => p.Id == result.Id);
                    StatusMessage = $"Updated profile: {result.Name}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error updating profile: {ex.Message}";
                }
            }
        }
    }

    [RelayCommand]
    private async Task DeleteProfile()
    {
        if (SelectedProfile == null) return;

        var name = SelectedProfile.Name;
        var id = SelectedProfile.Id;

        try
        {
            await _profileService.DeleteProfileAsync(id);
            SelectedProfile = null;
            await RefreshProfilesAsync();
            StatusMessage = $"Deleted profile: {name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting profile: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AttachSession()
    {
        if (SelectedProfile == null || _currentSession == null) return;

        try
        {
            await _profileService.AttachSessionAsync(SelectedProfile.Id, _currentSession.Id);
            await LoadProfileDetailsAsync(SelectedProfile.Id);
            StatusMessage = "Session attached to profile";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error attaching session: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportProfile()
    {
        if (SelectedProfile == null) return;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        try
        {
            var json = await _profileService.ExportProfileAsync(SelectedProfile.Id);
            
            var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Profile",
                DefaultExtension = "json",
                SuggestedFileName = $"{SelectedProfile.Name}-profile.json",
                FileTypeChoices =
                [
                    new FilePickerFileType("JSON") { Patterns = ["*.json"] }
                ]
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new System.IO.StreamWriter(stream);
                await writer.WriteAsync(json);
                StatusMessage = $"Exported profile to {file.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting profile: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportProfile()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        try
        {
            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Profile",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("JSON") { Patterns = ["*.json"] }
                ]
            });

            if (files.Count > 0)
            {
                await using var stream = await files[0].OpenReadAsync();
                using var reader = new System.IO.StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                
                var imported = await _profileService.ImportProfileAsync(json);
                await RefreshProfilesAsync();
                SelectedProfile = Profiles.FirstOrDefault(p => p.Id == imported.Id);
                StatusMessage = $"Imported profile: {imported.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing profile: {ex.Message}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the current session available for attachment.
    /// Call this after analyzing a combat log.
    /// </summary>
    public void SetCurrentSession(ExtendedCombatStatistics? session)
    {
        _currentSession = session;
        OnPropertyChanged(nameof(HasCurrentSession));
    }

    /// <summary>
    /// Suggests a profile for the current session.
    /// </summary>
    public async Task<CharacterProfile?> SuggestProfileForCurrentSessionAsync()
    {
        if (_currentSession == null) return null;
        return await _profileService.SuggestProfileForSessionAsync(_currentSession);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────────────────────────

    private async Task RefreshProfilesAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading profiles...";

        try
        {
            var allProfiles = await _profileService.GetAllProfilesAsync();
            
            // Apply filter
            var filtered = string.IsNullOrWhiteSpace(FilterText)
                ? allProfiles
                : allProfiles.Where(p => 
                    p.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                    p.Class.ToString().Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                    p.Realm.ToString().Contains(FilterText, StringComparison.OrdinalIgnoreCase));

            Profiles = new ObservableCollection<CharacterProfile>(filtered.OrderBy(p => p.Realm).ThenBy(p => p.Name));
            
            OnPropertyChanged(nameof(HasProfiles));
            OnPropertyChanged(nameof(GroupedProfiles));
            
            StatusMessage = $"{Profiles.Count} profile(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading profiles: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadProfileDetailsAsync(Guid profileId)
    {
        try
        {
            var sessionIds = await _profileService.GetAttachedSessionIdsAsync(profileId);
            
            AttachedSessions.Clear();
            foreach (var sessionId in sessionIds)
            {
                var session = await _sessionService.GetSessionAsync(sessionId);
                if (session != null)
                {
                    AttachedSessions.Add(CombatSessionSummary.FromExtended(session));
                }
            }

            PerformanceMetrics = SelectedProfile?.ActiveBuild?.PerformanceMetrics;

            OnPropertyChanged(nameof(HasAttachedSessions));
            OnPropertyChanged(nameof(HasPerformanceMetrics));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading sessions: {ex.Message}";
        }
    }

    private static Window? GetMainWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is 
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
            ? desktop.MainWindow 
            : null;
    }
}

/// <summary>
/// Group of profiles by realm for TreeView display.
/// </summary>
public record RealmProfileGroup(string RealmName, IReadOnlyList<CharacterProfile> Profiles);
