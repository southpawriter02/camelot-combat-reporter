namespace CamelotCombatReporter.Core.Alerts.Services;

/// <summary>
/// Platform-agnostic interface for text-to-speech functionality.
/// </summary>
public interface ITtsService
{
    /// <summary>
    /// Speaks the given text.
    /// </summary>
    /// <param name="text">Text to speak.</param>
    /// <param name="rate">Speech rate multiplier (0.5 to 2.0, default 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SpeakAsync(string text, float rate = 1.0f, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops any currently playing speech.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets or sets whether TTS is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Gets whether TTS is available on this platform.
    /// </summary>
    bool IsAvailable { get; }
}
