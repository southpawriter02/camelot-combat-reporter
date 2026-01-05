using CamelotCombatReporter.Core.Alerts;
using CamelotCombatReporter.Core.Alerts.Conditions;
using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Alerts.Notifications;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Tests;

public class AlertEngineTests
{
    [Fact]
    public void AddRule_ValidRule_AddsToEngine()
    {
        // Arrange
        var engine = new AlertEngine();
        var rule = CreateTestRule();

        // Act
        engine.AddRule(rule);

        // Assert
        Assert.Single(engine.Rules);
        Assert.Equal(rule.Id, engine.Rules[0].Id);
    }

    [Fact]
    public void RemoveRule_ExistingRule_RemovesFromEngine()
    {
        // Arrange
        var engine = new AlertEngine();
        var rule = CreateTestRule();
        engine.AddRule(rule);

        // Act
        engine.RemoveRule(rule.Id);

        // Assert
        Assert.Empty(engine.Rules);
    }

    [Fact]
    public void SetRuleState_ValidRule_UpdatesState()
    {
        // Arrange
        var engine = new AlertEngine();
        var rule = CreateTestRule();
        engine.AddRule(rule);

        // Act
        engine.SetRuleState(rule.Id, AlertRuleState.Paused);

        // Assert
        Assert.Equal(AlertRuleState.Paused, engine.Rules[0].State);
    }

    [Fact]
    public void CurrentState_InitialState_HasDefaultValues()
    {
        // Arrange
        var engine = new AlertEngine();

        // Act
        var state = engine.CurrentState;

        // Assert
        Assert.Equal(100.0, state.CurrentHealthPercent);
        Assert.Equal(100.0, state.CurrentEndurancePercent);
        Assert.Equal(100.0, state.CurrentPowerPercent);
        Assert.False(state.IsInCombat);
        Assert.Equal(0, state.KillStreak);
        Assert.Equal(0, state.DeathStreak);
    }

    [Fact]
    public void ResetSession_ResetsAllState()
    {
        // Arrange
        var engine = new AlertEngine();

        // Act
        engine.ResetSession();

        // Assert
        var state = engine.CurrentState;
        Assert.Equal(100.0, state.CurrentHealthPercent);
        Assert.Equal(0, state.KillStreak);
        Assert.False(state.IsInCombat);
    }

    [Fact]
    public void ClearTriggerHistory_RemovesAllTriggers()
    {
        // Arrange
        var engine = new AlertEngine();

        // Act
        engine.ClearTriggerHistory();

        // Assert
        Assert.Empty(engine.TriggerHistory);
    }

    [Fact]
    public void HealthBelowCondition_AboveThreshold_DoesNotTrigger()
    {
        // Arrange
        var condition = new HealthBelowCondition(30);
        var state = new CombatState { CurrentHealthPercent = 50.0 };

        // Act
        var (isMet, _, _) = condition.Evaluate(state);

        // Assert
        Assert.False(isMet);
    }

    [Fact]
    public void HealthBelowCondition_BelowThreshold_Triggers()
    {
        // Arrange
        var condition = new HealthBelowCondition(30);
        var state = new CombatState { CurrentHealthPercent = 25.0 };

        // Act
        var (isMet, reason, _) = condition.Evaluate(state);

        // Assert
        Assert.True(isMet);
        Assert.Contains("25", reason);
    }

    [Fact]
    public void KillStreakCondition_BelowThreshold_DoesNotTrigger()
    {
        // Arrange
        var condition = new KillStreakCondition(3);
        var state = new CombatState { KillStreak = 2 };

        // Act
        var (isMet, _, _) = condition.Evaluate(state);

        // Assert
        Assert.False(isMet);
    }

    [Fact]
    public void KillStreakCondition_AtOrAboveThreshold_Triggers()
    {
        // Arrange
        var condition = new KillStreakCondition(3);
        var state = new CombatState { KillStreak = 3 };

        // Act
        var (isMet, reason, _) = condition.Evaluate(state);

        // Assert
        Assert.True(isMet);
        Assert.Contains("3", reason);
    }

    [Fact]
    public void DamageInWindowCondition_BelowThreshold_DoesNotTrigger()
    {
        // Arrange
        var condition = new DamageInWindowCondition(500, TimeSpan.FromSeconds(3));
        var state = new CombatState { RecentDamageReceived = 300 };

        // Act
        var (isMet, _, _) = condition.Evaluate(state);

        // Assert
        Assert.False(isMet);
    }

    [Fact]
    public void DamageInWindowCondition_AboveThreshold_Triggers()
    {
        // Arrange
        var condition = new DamageInWindowCondition(500, TimeSpan.FromSeconds(3));
        var state = new CombatState { RecentDamageReceived = 600 };

        // Act
        var (isMet, reason, _) = condition.Evaluate(state);

        // Assert
        Assert.True(isMet);
        Assert.Contains("600", reason);
    }

    [Fact]
    public void CombatState_RecordDamageReceived_UpdatesRecentDamage()
    {
        // Arrange
        var state = new CombatState();
        var timestamp = TimeOnly.FromDateTime(DateTime.Now);

        // Act
        state.RecordDamageReceived(timestamp, 100);
        state.RecordDamageReceived(timestamp.Add(TimeSpan.FromSeconds(1)), 150);

        // Assert
        Assert.Equal(250, state.RecentDamageReceived);
    }

    [Fact]
    public void CombatState_RecordKill_IncrementsKillStreak()
    {
        // Arrange
        var state = new CombatState();

        // Act
        state.RecordKill();
        state.RecordKill();

        // Assert
        Assert.Equal(2, state.KillStreak);
        Assert.Equal(0, state.DeathStreak);
    }

    [Fact]
    public void CombatState_RecordDeath_ResetsKillStreak()
    {
        // Arrange
        var state = new CombatState { KillStreak = 5 };

        // Act
        state.RecordDeath();

        // Assert
        Assert.Equal(0, state.KillStreak);
        Assert.Equal(1, state.DeathStreak);
    }

    [Fact]
    public void EnemyClassCondition_MatchingClass_Triggers()
    {
        // Arrange
        var condition = new EnemyClassCondition(new[] { "Minstrel", "Cleric" });
        var state = new CombatState { CurrentTargetClass = "Minstrel" };

        // Act
        var (isMet, _, _) = condition.Evaluate(state);

        // Assert
        Assert.True(isMet);
    }

    [Fact]
    public void EnemyClassCondition_NonMatchingClass_DoesNotTrigger()
    {
        // Arrange
        var condition = new EnemyClassCondition(new[] { "Minstrel", "Cleric" });
        var state = new CombatState { CurrentTargetClass = "Scout" };

        // Act
        var (isMet, _, _) = condition.Evaluate(state);

        // Assert
        Assert.False(isMet);
    }

    [Fact]
    public void DebuffAppliedCondition_MatchingDebuff_Triggers()
    {
        // Arrange
        var condition = new DebuffAppliedCondition(new[] { "Disease", "Poison" });
        var state = new CombatState { ActiveDebuffs = new List<string> { "Disease Effect" } };

        // Act
        var (isMet, _, _) = condition.Evaluate(state);

        // Assert
        Assert.True(isMet);
    }

    private static AlertRule CreateTestRule() => new(
        Id: Guid.NewGuid(),
        Name: "Test Rule",
        Description: "Test Description",
        Priority: AlertPriority.Medium,
        Logic: ConditionLogic.And,
        Conditions: Array.Empty<IAlertCondition>(),
        Notifications: Array.Empty<INotification>(),
        Cooldown: TimeSpan.FromSeconds(5));
}
