namespace CamelotCombatReporter.Core.Alerts.Services;

/// <summary>
/// Platform-agnostic interface for audio playback.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Plays a sound file.
    /// </summary>
    /// <param name="soundPath">Path to the sound file.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PlaySoundAsync(string soundPath, float volume = 1.0f, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the master volume for all audio playback.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    void SetMasterVolume(float volume);

    /// <summary>
    /// Gets or sets whether audio is muted.
    /// </summary>
    bool IsMuted { get; set; }

    /// <summary>
    /// Stops all currently playing sounds.
    /// </summary>
    void StopAll();
}
