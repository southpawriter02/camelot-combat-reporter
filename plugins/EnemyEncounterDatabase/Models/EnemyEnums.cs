namespace EnemyEncounterDatabase.Models;

/// <summary>
/// Classifies the type of enemy encountered in combat.
/// </summary>
/// <remarks>
/// <para>
/// Enemy classification is determined heuristically by the <see cref="Analysis.EncounterAnalyzer"/>
/// based on naming patterns observed in Dark Age of Camelot combat logs.
/// </para>
/// <para>
/// Classification rules:
/// <list type="bullet">
///   <item><description><see cref="Player"/>: Single capitalized word (e.g., "Nemesis", "Darkblade")</description></item>
///   <item><description><see cref="Mob"/>: Multi-word names, articles, or lowercase (e.g., "a skeletal warrior", "forest wolf")</description></item>
///   <item><description><see cref="NPC"/>: Guards, merchants, and other special NPCs</description></item>
///   <item><description><see cref="Unknown"/>: Cannot be reliably classified</description></item>
/// </list>
/// </para>
/// </remarks>
public enum EnemyType
{
    /// <summary>
    /// Unknown entity type. Used when classification heuristics cannot determine the enemy type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// AI-controlled monster or creature. Includes all PvE enemies.
    /// </summary>
    /// <example>forest wolf, a skeletal warrior, Lich Lord</example>
    Mob = 1,

    /// <summary>
    /// Player character from an opposing realm in RvR combat.
    /// </summary>
    /// <example>Darkblade, Nemesis, Shadowmage</example>
    Player = 2,

    /// <summary>
    /// Non-player character such as guards, merchants, or quest NPCs.
    /// </summary>
    /// <example>Realm Guard, Keep Lord</example>
    NPC = 3
}

/// <summary>
/// The outcome of a combat encounter with an enemy.
/// </summary>
/// <remarks>
/// <para>
/// Outcome is determined by analyzing death events in the combat log.
/// If no death event is found for either party, the outcome defaults to <see cref="Unknown"/>.
/// </para>
/// <para>
/// Win/loss statistics are calculated based on <see cref="Victory"/> and <see cref="Defeat"/> outcomes.
/// <see cref="Escaped"/> encounters do not affect the win rate calculation.
/// </para>
/// </remarks>
public enum EncounterOutcome
{
    /// <summary>
    /// Outcome could not be determined from the combat log.
    /// </summary>
    /// <remarks>
    /// This is the default when no death events are found.
    /// May occur if combat ended due to zoning, fleeing beyond log range, or log truncation.
    /// </remarks>
    Unknown = 0,

    /// <summary>
    /// You killed the enemy. Increments the kill count for this enemy.
    /// </summary>
    Victory = 1,

    /// <summary>
    /// The enemy killed you. Increments the death count for this enemy.
    /// </summary>
    Defeat = 2,

    /// <summary>
    /// Combat ended without either party dying.
    /// </summary>
    /// <remarks>
    /// This may occur when one party flees successfully, combat is interrupted,
    /// or the enemy despawns/evades.
    /// </remarks>
    Escaped = 3
}
