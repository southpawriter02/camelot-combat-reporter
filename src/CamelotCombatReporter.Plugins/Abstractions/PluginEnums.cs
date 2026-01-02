namespace CamelotCombatReporter.Plugins.Abstractions;

/// <summary>
/// Types of plugins supported by the system.
/// </summary>
public enum PluginType
{
    /// <summary>
    /// Plugins that provide custom statistics and metrics analysis.
    /// </summary>
    DataAnalysis,

    /// <summary>
    /// Plugins that add new export formats (XML, HTML, PDF, etc.).
    /// </summary>
    ExportFormat,

    /// <summary>
    /// Plugins that add UI components like tabs, panels, or visualizations.
    /// </summary>
    UIComponent,

    /// <summary>
    /// Plugins that add new log parsing patterns or event types.
    /// </summary>
    CustomParser
}

/// <summary>
/// Plugin lifecycle states.
/// </summary>
public enum PluginState
{
    /// <summary>Plugin has not been loaded.</summary>
    Unloaded,

    /// <summary>Plugin is being loaded.</summary>
    Loading,

    /// <summary>Plugin has been loaded but not initialized.</summary>
    Loaded,

    /// <summary>Plugin is being initialized.</summary>
    Initializing,

    /// <summary>Plugin has been initialized and is ready.</summary>
    Initialized,

    /// <summary>Plugin is enabled and active.</summary>
    Enabled,

    /// <summary>Plugin is disabled.</summary>
    Disabled,

    /// <summary>Plugin encountered an error.</summary>
    Error,

    /// <summary>Plugin is being unloaded.</summary>
    Unloading
}
