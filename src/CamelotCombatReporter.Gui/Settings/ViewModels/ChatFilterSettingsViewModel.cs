using System;
using System.Collections.ObjectModel;
using System.Linq;
using CamelotCombatReporter.Core.ChatFiltering;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.Settings.ViewModels;

/// <summary>
/// ViewModel for Chat Filter settings.
/// </summary>
public partial class ChatFilterSettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _selectedPreset = "All Messages";

    public string[] PresetOptions { get; } = new[]
    {
        "All Messages",
        "Combat Only",
        "Tactical",
        "Custom"
    };

    [ObservableProperty]
    private ObservableCollection<ChannelSettingViewModel> _channelSettings = new();

    [ObservableProperty]
    private string _keywordWhitelist = "";

    [ObservableProperty]
    private string _senderWhitelist = "";

    [ObservableProperty]
    private int _combatContextWindow = 10;

    [ObservableProperty]
    private bool _hasChanges;

    public ChatFilterSettingsViewModel()
    {
        InitializeChannelSettings();
    }

    private void InitializeChannelSettings()
    {
        ChannelSettings.Clear();

        // Add all chat message types
        foreach (ChatMessageType type in Enum.GetValues<ChatMessageType>())
        {
            ChannelSettings.Add(new ChannelSettingViewModel(type)
            {
                IsEnabled = true,
                KeepDuringCombat = type == ChatMessageType.Group || type == ChatMessageType.Guild
            });
        }
    }

    partial void OnSelectedPresetChanged(string value)
    {
        ApplyPreset(value);
        HasChanges = true;
    }

    private void ApplyPreset(string preset)
    {
        switch (preset)
        {
            case "Combat Only":
                foreach (var channel in ChannelSettings)
                {
                    channel.IsEnabled = channel.Type == ChatMessageType.Combat;
                }
                break;

            case "Tactical":
                foreach (var channel in ChannelSettings)
                {
                    channel.IsEnabled = channel.Type is
                        ChatMessageType.Group or
                        ChatMessageType.Guild or
                        ChatMessageType.Alliance or
                        ChatMessageType.Combat;
                    channel.KeepDuringCombat = channel.Type == ChatMessageType.Group;
                }
                break;

            case "All Messages":
                foreach (var channel in ChannelSettings)
                {
                    channel.IsEnabled = true;
                    channel.KeepDuringCombat = false;
                }
                break;

            case "Custom":
                // Don't change anything - user is customizing
                break;
        }
    }

    [RelayCommand]
    private void EnableAll()
    {
        foreach (var channel in ChannelSettings)
        {
            channel.IsEnabled = true;
        }
        SelectedPreset = "All Messages";
        HasChanges = true;
    }

    [RelayCommand]
    private void DisableAll()
    {
        foreach (var channel in ChannelSettings)
        {
            channel.IsEnabled = false;
        }
        SelectedPreset = "Custom";
        HasChanges = true;
    }

    public void Save()
    {
        // Save settings to configuration
        HasChanges = false;
    }

    public void ResetToDefaults()
    {
        IsEnabled = false;
        SelectedPreset = "All Messages";
        KeywordWhitelist = "";
        SenderWhitelist = "";
        CombatContextWindow = 10;
        InitializeChannelSettings();
        HasChanges = false;
    }

    public ChatFilterSettings ToSettings()
    {
        var preset = SelectedPreset switch
        {
            "Combat Only" => FilterPreset.CombatOnly,
            "Tactical" => FilterPreset.Tactical,
            "Custom" => FilterPreset.Custom,
            _ => FilterPreset.AllMessages
        };

        var channelConfigs = ChannelSettings.ToDictionary(
            c => c.Type,
            c => new ChannelConfig(c.IsEnabled, c.KeepDuringCombat, null)
        );

        var keywords = KeywordWhitelist
            .Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrEmpty(k))
            .ToHashSet();

        var senders = SenderWhitelist
            .Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet();

        return new ChatFilterSettings
        {
            Enabled = IsEnabled,
            Preset = preset,
            ChannelSettings = channelConfigs,
            KeywordWhitelist = keywords,
            SenderWhitelist = senders,
            CombatContextWindowSeconds = CombatContextWindow
        };
    }
}

/// <summary>
/// ViewModel for a single channel setting.
/// </summary>
public partial class ChannelSettingViewModel : ViewModelBase
{
    public ChatMessageType Type { get; }
    public string Name => Type.ToString();

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _keepDuringCombat;

    public ChannelSettingViewModel(ChatMessageType type)
    {
        Type = type;
    }

    public string Description => Type switch
    {
        ChatMessageType.Say => "Local area chat",
        ChatMessageType.Yell => "Extended range chat",
        ChatMessageType.Group => "Party/group chat",
        ChatMessageType.Guild => "Guild chat",
        ChatMessageType.Alliance => "Alliance chat",
        ChatMessageType.Broadcast => "Realm-wide broadcast",
        ChatMessageType.Send => "Private messages sent",
        ChatMessageType.Tell => "Private messages received",
        ChatMessageType.Region => "Region/zone chat",
        ChatMessageType.Trade => "Trade channel",
        ChatMessageType.Emote => "Player emotes",
        ChatMessageType.NpcDialog => "NPC conversations",
        ChatMessageType.Combat => "Combat messages",
        ChatMessageType.LFG => "Looking for group",
        ChatMessageType.Advice => "Advice channel",
        ChatMessageType.System => "System messages",
        _ => "Unknown channel"
    };
}
