using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CamelotCombatReporter.Core.Alerts;
using CamelotCombatReporter.Core.Alerts.Conditions;
using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Alerts.Notifications;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.Alerts.ViewModels;

/// <summary>
/// ViewModel for the Alerts view.
/// </summary>
public partial class AlertsViewModel : ViewModelBase
{
    private readonly AlertEngine _alertEngine;
    private readonly IAlertConfigurationService _configService;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<AlertRuleViewModel> _rules = new();

    [ObservableProperty]
    private AlertRuleViewModel? _selectedRule;

    [ObservableProperty]
    private ObservableCollection<AlertTriggerViewModel> _recentTriggers = new();

    [ObservableProperty]
    private bool _isGlobalMuted;

    [ObservableProperty]
    private float _masterVolume = 1.0f;

    [ObservableProperty]
    private bool _isTtsEnabled = true;

    [ObservableProperty]
    private bool _isAlertsEnabled = true;

    [ObservableProperty]
    private string _statusMessage = "Alerts system ready";

    [ObservableProperty]
    private int _totalTriggersToday;

    [ObservableProperty]
    private int _activeRulesCount;

    #endregion

    public AlertsViewModel()
    {
        _alertEngine = new AlertEngine();
        _configService = new AlertConfigurationService(GetDefaultConfigPath());

        _alertEngine.AlertTriggered += OnAlertTriggered;

        LoadDefaultRules();
    }

    public AlertsViewModel(AlertEngine alertEngine, IAlertConfigurationService configService)
    {
        _alertEngine = alertEngine;
        _configService = configService;

        _alertEngine.AlertTriggered += OnAlertTriggered;
    }

    /// <summary>
    /// Gets the alert engine for integration.
    /// </summary>
    public AlertEngine AlertEngine => _alertEngine;

    #region Commands

    /// <summary>
    /// Adds a new alert rule.
    /// </summary>
    [RelayCommand]
    private void AddRule()
    {
        var newRule = new AlertRule(
            Id: Guid.NewGuid(),
            Name: "New Alert",
            Description: "Configure this alert",
            Priority: AlertPriority.Medium,
            Logic: ConditionLogic.And,
            Conditions: Array.Empty<IAlertCondition>(),
            Notifications: Array.Empty<INotification>(),
            Cooldown: TimeSpan.FromSeconds(5));

        _alertEngine.AddRule(newRule);
        var vm = new AlertRuleViewModel(newRule);
        Rules.Add(vm);
        SelectedRule = vm;
        UpdateActiveRulesCount();
    }

    /// <summary>
    /// Removes the selected rule.
    /// </summary>
    [RelayCommand]
    private void RemoveRule()
    {
        if (SelectedRule == null)
            return;

        _alertEngine.RemoveRule(SelectedRule.Id);
        Rules.Remove(SelectedRule);
        SelectedRule = null;
        UpdateActiveRulesCount();
    }

    /// <summary>
    /// Duplicates the selected rule.
    /// </summary>
    [RelayCommand]
    private void DuplicateRule()
    {
        if (SelectedRule == null)
            return;

        var original = SelectedRule.Rule;
        var duplicate = original with
        {
            Id = Guid.NewGuid(),
            Name = $"{original.Name} (Copy)"
        };

        _alertEngine.AddRule(duplicate);
        var vm = new AlertRuleViewModel(duplicate);
        Rules.Add(vm);
        SelectedRule = vm;
        UpdateActiveRulesCount();
    }

    /// <summary>
    /// Toggles the enabled state of a rule.
    /// </summary>
    [RelayCommand]
    private void ToggleRule(AlertRuleViewModel? ruleVm)
    {
        if (ruleVm == null)
            return;

        var newState = ruleVm.State == AlertRuleState.Active
            ? AlertRuleState.Paused
            : AlertRuleState.Active;

        _alertEngine.SetRuleState(ruleVm.Id, newState);
        ruleVm.State = newState;
        UpdateActiveRulesCount();
    }

    /// <summary>
    /// Tests the selected rule by simulating a trigger.
    /// </summary>
    [RelayCommand]
    private void TestRule()
    {
        if (SelectedRule == null)
            return;

        var testContext = new AlertContext(
            SelectedRule.Rule,
            TimeOnly.FromDateTime(DateTime.Now),
            "Test trigger",
            new System.Collections.Generic.Dictionary<string, object>(),
            null);

        AddTriggerToHistory(testContext, isTest: true);
        StatusMessage = $"Tested rule: {SelectedRule.Name}";
    }

    /// <summary>
    /// Clears all trigger history.
    /// </summary>
    [RelayCommand]
    private void ClearHistory()
    {
        _alertEngine.ClearTriggerHistory();
        RecentTriggers.Clear();
        TotalTriggersToday = 0;
        StatusMessage = "Trigger history cleared";
    }

    /// <summary>
    /// Saves the current configuration.
    /// </summary>
    [RelayCommand]
    private async Task SaveConfiguration()
    {
        try
        {
            var rules = Rules.Select(r => r.Rule).ToList();
            var config = _configService.SerializeRules(rules, IsGlobalMuted, MasterVolume, IsTtsEnabled);
            await _configService.SaveAsync(config);
            StatusMessage = "Configuration saved";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Loads configuration from storage.
    /// </summary>
    [RelayCommand]
    private async Task LoadConfiguration()
    {
        try
        {
            var config = await _configService.LoadAsync();
            var rules = _configService.DeserializeRules(config);

            _alertEngine.ClearRules();
            Rules.Clear();

            foreach (var rule in rules)
            {
                _alertEngine.AddRule(rule);
                Rules.Add(new AlertRuleViewModel(rule));
            }

            IsGlobalMuted = config.GlobalMute;
            MasterVolume = config.MasterVolume;
            IsTtsEnabled = config.TtsEnabled;

            UpdateActiveRulesCount();
            StatusMessage = "Configuration loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Adds a preset rule for low health alert.
    /// </summary>
    [RelayCommand]
    private void AddLowHealthPreset()
    {
        var rule = new AlertRule(
            Id: Guid.NewGuid(),
            Name: "Low Health Warning",
            Description: "Triggers when health drops below 30%",
            Priority: AlertPriority.Critical,
            Logic: ConditionLogic.And,
            Conditions: new IAlertCondition[]
            {
                new HealthBelowCondition(30)
            },
            Notifications: Array.Empty<INotification>(),
            Cooldown: TimeSpan.FromSeconds(3));

        _alertEngine.AddRule(rule);
        Rules.Add(new AlertRuleViewModel(rule));
        UpdateActiveRulesCount();
        StatusMessage = "Added Low Health preset";
    }

    /// <summary>
    /// Adds a preset rule for kill streak alert.
    /// </summary>
    [RelayCommand]
    private void AddKillStreakPreset()
    {
        var rule = new AlertRule(
            Id: Guid.NewGuid(),
            Name: "Kill Streak",
            Description: "Triggers on 3+ kill streak",
            Priority: AlertPriority.Low,
            Logic: ConditionLogic.And,
            Conditions: new IAlertCondition[]
            {
                new KillStreakCondition(3)
            },
            Notifications: Array.Empty<INotification>(),
            Cooldown: TimeSpan.FromSeconds(10));

        _alertEngine.AddRule(rule);
        Rules.Add(new AlertRuleViewModel(rule));
        UpdateActiveRulesCount();
        StatusMessage = "Added Kill Streak preset";
    }

    /// <summary>
    /// Adds a preset rule for burst damage alert.
    /// </summary>
    [RelayCommand]
    private void AddBurstDamagePreset()
    {
        var rule = new AlertRule(
            Id: Guid.NewGuid(),
            Name: "Burst Damage Alert",
            Description: "Triggers when receiving heavy burst damage",
            Priority: AlertPriority.High,
            Logic: ConditionLogic.And,
            Conditions: new IAlertCondition[]
            {
                new DamageInWindowCondition(500, TimeSpan.FromSeconds(3))
            },
            Notifications: Array.Empty<INotification>(),
            Cooldown: TimeSpan.FromSeconds(5));

        _alertEngine.AddRule(rule);
        Rules.Add(new AlertRuleViewModel(rule));
        UpdateActiveRulesCount();
        StatusMessage = "Added Burst Damage preset";
    }

    #endregion

    #region Event Handlers

    private void OnAlertTriggered(object? sender, AlertTriggeredEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            AddTriggerToHistory(e.Context, isTest: false);
        });
    }

    private void AddTriggerToHistory(AlertContext context, bool isTest)
    {
        var trigger = new AlertTriggerViewModel(context, isTest);
        RecentTriggers.Insert(0, trigger);

        // Keep only last 50 triggers
        while (RecentTriggers.Count > 50)
        {
            RecentTriggers.RemoveAt(RecentTriggers.Count - 1);
        }

        if (!isTest)
        {
            TotalTriggersToday++;
        }
    }

    #endregion

    #region Private Methods

    private void LoadDefaultRules()
    {
        // Add some sensible default rules
        AddLowHealthPreset();
        AddBurstDamagePreset();
    }

    private void UpdateActiveRulesCount()
    {
        ActiveRulesCount = Rules.Count(r => r.State == AlertRuleState.Active);
    }

    private static string GetDefaultConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return System.IO.Path.Combine(appData, "CamelotCombatReporter", "alerts-config.json");
    }

    #endregion
}

/// <summary>
/// ViewModel for displaying an alert rule.
/// </summary>
public partial class AlertRuleViewModel : ObservableObject
{
    public AlertRuleViewModel(AlertRule rule)
    {
        Rule = rule;
        Id = rule.Id;
        Name = rule.Name;
        Description = rule.Description;
        Priority = rule.Priority;
        State = rule.State;
        ConditionCount = rule.Conditions.Count;
        NotificationCount = rule.Notifications.Count;
        Cooldown = rule.Cooldown;
        Logic = rule.Logic;
    }

    public AlertRule Rule { get; private set; }
    public Guid Id { get; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    [ObservableProperty]
    private AlertPriority _priority;

    [ObservableProperty]
    private AlertRuleState _state;

    [ObservableProperty]
    private int _conditionCount;

    [ObservableProperty]
    private int _notificationCount;

    [ObservableProperty]
    private TimeSpan _cooldown;

    [ObservableProperty]
    private ConditionLogic _logic;

    public string PriorityColor => Priority switch
    {
        AlertPriority.Critical => "#FF0000",
        AlertPriority.High => "#FF6600",
        AlertPriority.Medium => "#FFCC00",
        AlertPriority.Low => "#00CC00",
        _ => "#808080"
    };

    public string StateIcon => State switch
    {
        AlertRuleState.Active => "Active",
        AlertRuleState.Paused => "Paused",
        AlertRuleState.Disabled => "Disabled",
        _ => "?"
    };

    public bool IsActive => State == AlertRuleState.Active;

    public string ConditionsSummary => Rule.Conditions.Count switch
    {
        0 => "No conditions",
        1 => Rule.Conditions[0].Description,
        _ => $"{Rule.Conditions.Count} conditions ({Logic})"
    };
}

/// <summary>
/// ViewModel for displaying an alert trigger in history.
/// </summary>
public class AlertTriggerViewModel
{
    public AlertTriggerViewModel(AlertContext context, bool isTest)
    {
        RuleName = context.Rule.Name;
        Priority = context.Rule.Priority;
        Timestamp = context.Timestamp.ToString("HH:mm:ss");
        Reason = context.TriggerReason;
        IsTest = isTest;
    }

    public string RuleName { get; }
    public AlertPriority Priority { get; }
    public string Timestamp { get; }
    public string Reason { get; }
    public bool IsTest { get; }

    public string PriorityColor => Priority switch
    {
        AlertPriority.Critical => "#FF0000",
        AlertPriority.High => "#FF6600",
        AlertPriority.Medium => "#FFCC00",
        AlertPriority.Low => "#00CC00",
        _ => "#808080"
    };

    public string TypeIndicator => IsTest ? "[TEST]" : "";
}
