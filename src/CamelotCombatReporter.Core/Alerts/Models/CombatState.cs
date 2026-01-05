namespace CamelotCombatReporter.Core.Alerts.Models;

/// <summary>
/// Real-time combat state for condition evaluation.
/// Updated continuously during live combat monitoring.
/// </summary>
public class CombatState
{
    #region Resource Percentages

    /// <summary>Current health as a percentage (0-100).</summary>
    public double CurrentHealthPercent { get; set; } = 100;

    /// <summary>Current endurance as a percentage (0-100).</summary>
    public double CurrentEndurancePercent { get; set; } = 100;

    /// <summary>Current power/mana as a percentage (0-100).</summary>
    public double CurrentPowerPercent { get; set; } = 100;

    #endregion

    #region Combat Status

    /// <summary>Whether the player is currently in combat.</summary>
    public bool IsInCombat { get; set; }

    /// <summary>When combat started, if in combat.</summary>
    public TimeOnly? CombatStartTime { get; set; }

    /// <summary>Timestamp of the last processed event.</summary>
    public TimeOnly LastEventTime { get; set; }

    #endregion

    #region Recent Combat Activity

    /// <summary>Total damage received within the damage window.</summary>
    public int RecentDamageReceived { get; set; }

    /// <summary>Total damage dealt within the damage window.</summary>
    public int RecentDamageDealt { get; set; }

    /// <summary>Total healing received within the damage window.</summary>
    public int RecentHealingReceived { get; set; }

    /// <summary>Time window for tracking "recent" damage (default 5 seconds).</summary>
    public TimeSpan DamageWindow { get; set; } = TimeSpan.FromSeconds(5);

    #endregion

    #region Target Information

    /// <summary>Name of the current target, if any.</summary>
    public string? CurrentTargetName { get; set; }

    /// <summary>Class of the current target, if known.</summary>
    public string? CurrentTargetClass { get; set; }

    /// <summary>Realm of the current target, if known.</summary>
    public string? CurrentTargetRealm { get; set; }

    #endregion

    #region Buff/Debuff Tracking

    /// <summary>Names of currently active debuffs on the player.</summary>
    public IReadOnlyList<string> ActiveDebuffs { get; set; } = Array.Empty<string>();

    /// <summary>Names of currently active buffs on the player.</summary>
    public IReadOnlyList<string> ActiveBuffs { get; set; } = Array.Empty<string>();

    /// <summary>Names of abilities currently on cooldown.</summary>
    public IReadOnlyList<string> AbilitiesOnCooldown { get; set; } = Array.Empty<string>();

    #endregion

    #region Kill/Death Tracking

    /// <summary>Total kills this session.</summary>
    public int KillCount { get; set; }

    /// <summary>Total deaths this session.</summary>
    public int DeathCount { get; set; }

    /// <summary>Current consecutive kill streak.</summary>
    public int KillStreak { get; set; }

    /// <summary>Current consecutive death streak.</summary>
    public int DeathStreak { get; set; }

    #endregion

    #region History Queues

    /// <summary>Recent damage events for windowed calculations.</summary>
    public Queue<(TimeOnly Time, int Amount)> DamageHistory { get; } = new();

    /// <summary>Recent ability usage for tracking.</summary>
    public Queue<(TimeOnly Time, string Ability)> AbilityHistory { get; } = new();

    /// <summary>Recent healing events for windowed calculations.</summary>
    public Queue<(TimeOnly Time, int Amount)> HealingHistory { get; } = new();

    #endregion

    #region Methods

    /// <summary>
    /// Records damage received and updates windowed damage total.
    /// </summary>
    /// <param name="time">When the damage occurred.</param>
    /// <param name="amount">Amount of damage received.</param>
    public void RecordDamageReceived(TimeOnly time, int amount)
    {
        DamageHistory.Enqueue((time, amount));
        PruneHistory(DamageHistory, time);
        RecentDamageReceived = DamageHistory.Sum(d => d.Amount);
    }

    /// <summary>
    /// Records damage dealt.
    /// </summary>
    /// <param name="time">When the damage occurred.</param>
    /// <param name="amount">Amount of damage dealt.</param>
    public void RecordDamageDealt(TimeOnly time, int amount)
    {
        RecentDamageDealt += amount;
    }

    /// <summary>
    /// Records healing received and updates windowed healing total.
    /// </summary>
    /// <param name="time">When the healing occurred.</param>
    /// <param name="amount">Amount of healing received.</param>
    public void RecordHealingReceived(TimeOnly time, int amount)
    {
        HealingHistory.Enqueue((time, amount));
        PruneHistory(HealingHistory, time);
        RecentHealingReceived = HealingHistory.Sum(h => h.Amount);
    }

    /// <summary>
    /// Records an ability usage.
    /// </summary>
    /// <param name="time">When the ability was used.</param>
    /// <param name="abilityName">Name of the ability.</param>
    public void RecordAbilityUsed(TimeOnly time, string abilityName)
    {
        AbilityHistory.Enqueue((time, abilityName));

        // Keep only last 20 abilities
        while (AbilityHistory.Count > 20)
            AbilityHistory.Dequeue();
    }

    /// <summary>
    /// Records a kill and updates streak tracking.
    /// </summary>
    public void RecordKill()
    {
        KillCount++;
        KillStreak++;
        DeathStreak = 0;
    }

    /// <summary>
    /// Records a death and updates streak tracking.
    /// </summary>
    public void RecordDeath()
    {
        DeathCount++;
        DeathStreak++;
        KillStreak = 0;
    }

    /// <summary>
    /// Removes entries older than the damage window from a history queue.
    /// </summary>
    private void PruneHistory(Queue<(TimeOnly Time, int Amount)> history, TimeOnly currentTime)
    {
        while (history.Count > 0)
        {
            var oldest = history.Peek();
            var elapsed = currentTime.ToTimeSpan() - oldest.Time.ToTimeSpan();

            // Handle day rollover
            if (elapsed < TimeSpan.Zero)
                elapsed += TimeSpan.FromHours(24);

            if (elapsed > DamageWindow)
                history.Dequeue();
            else
                break;
        }
    }

    /// <summary>
    /// Resets all combat state to initial values.
    /// </summary>
    public void Reset()
    {
        CurrentHealthPercent = 100;
        CurrentEndurancePercent = 100;
        CurrentPowerPercent = 100;

        IsInCombat = false;
        CombatStartTime = null;

        RecentDamageReceived = 0;
        RecentDamageDealt = 0;
        RecentHealingReceived = 0;

        CurrentTargetName = null;
        CurrentTargetClass = null;
        CurrentTargetRealm = null;

        ActiveDebuffs = Array.Empty<string>();
        ActiveBuffs = Array.Empty<string>();
        AbilitiesOnCooldown = Array.Empty<string>();

        KillCount = 0;
        DeathCount = 0;
        KillStreak = 0;
        DeathStreak = 0;

        DamageHistory.Clear();
        AbilityHistory.Clear();
        HealingHistory.Clear();
    }

    /// <summary>
    /// Resets session-specific counters while keeping combat state.
    /// </summary>
    public void ResetSessionCounters()
    {
        KillCount = 0;
        DeathCount = 0;
        KillStreak = 0;
        DeathStreak = 0;
    }

    #endregion
}
