using CamelotCombatReporter.Gui.ViewModels;
using Xunit;

namespace CamelotCombatReporter.Gui.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void InitialState_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert - Basic properties
        Assert.Equal("No file selected", viewModel.SelectedLogFile);
        Assert.Equal("You", viewModel.CombatantName);
        Assert.False(viewModel.HasAnalyzedData);
        Assert.Equal("0.00", viewModel.LogDuration);
        Assert.Equal(0, viewModel.TotalDamageDealt);
        Assert.Equal("0.00", viewModel.DamagePerSecond);
        Assert.Equal("0.00", viewModel.AverageDamage);
        Assert.Equal("0.00", viewModel.MedianDamage);
        Assert.Equal(0, viewModel.CombatStylesUsed);
        Assert.Equal(0, viewModel.SpellsCast);
    }

    [Fact]
    public void InitialState_ShouldHaveEventTypeTogglesEnabled()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert - Event type toggles should be enabled by default
        Assert.True(viewModel.ShowDamageDealt);
        Assert.True(viewModel.ShowDamageTaken);
        Assert.True(viewModel.ShowHealingDone);
        Assert.True(viewModel.ShowHealingReceived);
        Assert.True(viewModel.ShowCombatStyles);
        Assert.True(viewModel.ShowSpells);
    }

    [Fact]
    public void InitialState_ShouldHaveStatisticsTogglesEnabled()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert - Statistics visibility toggles
        Assert.True(viewModel.ShowDurationStat);
        Assert.True(viewModel.ShowTotalDamageStat);
        Assert.True(viewModel.ShowDpsStat);
        Assert.True(viewModel.ShowAverageDamageStat);
        Assert.True(viewModel.ShowMedianDamageStat);
        Assert.True(viewModel.ShowCombatStylesStat);
        Assert.True(viewModel.ShowSpellsCastStat);
        Assert.True(viewModel.ShowHealingStats);
        Assert.True(viewModel.ShowDamageTakenStats);
    }

    [Fact]
    public void InitialState_ShouldHaveDefaultChartOptions()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert - Chart options
        Assert.Equal("Line", viewModel.SelectedChartType);
        Assert.Equal("5s", viewModel.SelectedChartInterval);
        Assert.True(viewModel.ShowDamageDealtOnChart);
        Assert.False(viewModel.ShowDamageTakenOnChart);
        Assert.False(viewModel.ShowHealingOnChart);
        Assert.False(viewModel.ShowDpsTrendLine);
    }

    [Fact]
    public void InitialState_ShouldHaveDefaultFilterValues()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert - Filter defaults
        Assert.Equal("All", viewModel.SelectedDamageType);
        Assert.Equal("All", viewModel.SelectedTarget);
        Assert.False(viewModel.IsComparisonMode);
        Assert.Equal("No file selected", viewModel.ComparisonLogFile);
    }

    [Fact]
    public void InitialState_ShouldHaveHealingStatisticsDefaults()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert - Healing stats defaults
        Assert.Equal(0, viewModel.TotalHealingDone);
        Assert.Equal("0.00", viewModel.HealingPerSecond);
        Assert.Equal("0.00", viewModel.AverageHealing);
        Assert.Equal("0.00", viewModel.MedianHealing);
        Assert.Equal(0, viewModel.TotalHealingReceived);
        Assert.Equal("0.00", viewModel.HealingReceivedPerSecond);
    }

    [Fact]
    public void InitialState_ShouldHaveDamageTakenStatisticsDefaults()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert - Damage taken stats defaults
        Assert.Equal(0, viewModel.TotalDamageTaken);
        Assert.Equal("0.00", viewModel.DamageTakenPerSecond);
        Assert.Equal("0.00", viewModel.AverageDamageTaken);
        Assert.Equal("0.00", viewModel.MedianDamageTaken);
    }

    [Fact]
    public async Task AnalyzeLog_WithValidFile_ShouldUpdateStatistics()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Find repository root by looking for the .git directory or data folder
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var repoRoot = FindRepositoryRoot(currentDir);

        if (repoRoot == null)
        {
            return; // Skip test if repository root cannot be found
        }

        var testLogPath = Path.Combine(repoRoot, "data", "sample.log");

        // Skip test if sample.log doesn't exist
        if (!File.Exists(testLogPath))
        {
            return; // Skip test gracefully
        }

        viewModel.SelectedLogFile = testLogPath;

        // Act
        await viewModel.AnalyzeLogCommand.ExecuteAsync(null);

        // Assert
        Assert.True(viewModel.HasAnalyzedData);
        Assert.Equal(125, viewModel.TotalDamageDealt);
        Assert.True(viewModel.DamagePerSecond != "0.00");
        Assert.Equal("62.50", viewModel.AverageDamage);
        Assert.Equal("62.50", viewModel.MedianDamage);
        Assert.Equal(1, viewModel.CombatStylesUsed);
        Assert.Equal(2, viewModel.SpellsCast);
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            // Look for .git directory or data folder as markers of repository root
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
                Directory.Exists(Path.Combine(dir.FullName, "data")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    [Fact]
    public async Task AnalyzeLog_WithNoFileSelected_ShouldNotCrash()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act & Assert - should not throw
        await viewModel.AnalyzeLogCommand.ExecuteAsync(null);
        Assert.False(viewModel.HasAnalyzedData);
    }

    [Fact]
    public void CombatantName_CanBeChanged()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.CombatantName = "TestPlayer";

        // Assert
        Assert.Equal("TestPlayer", viewModel.CombatantName);
    }

    [Fact]
    public async Task AnalyzeLog_ShouldGenerateChartSeries()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var repoRoot = FindRepositoryRoot(currentDir);

        if (repoRoot == null) return;

        var testLogPath = Path.Combine(repoRoot, "data", "sample.log");

        if (!File.Exists(testLogPath)) return;

        viewModel.SelectedLogFile = testLogPath;

        // Act
        await viewModel.AnalyzeLogCommand.ExecuteAsync(null);

        // Assert
        Assert.True(viewModel.HasAnalyzedData);
        Assert.NotNull(viewModel.Series);
        Assert.NotEmpty(viewModel.Series);
    }

    [Fact]
    public async Task AnalyzeLog_ShouldPopulateFilters()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var repoRoot = FindRepositoryRoot(currentDir);

        if (repoRoot == null) return;

        var testLogPath = Path.Combine(repoRoot, "data", "sample.log");

        if (!File.Exists(testLogPath)) return;

        viewModel.SelectedLogFile = testLogPath;

        // Act
        await viewModel.AnalyzeLogCommand.ExecuteAsync(null);

        // Assert
        Assert.True(viewModel.HasAnalyzedData);
        Assert.NotNull(viewModel.AvailableDamageTypes);
        Assert.Contains("All", viewModel.AvailableDamageTypes);
        Assert.NotNull(viewModel.AvailableTargets);
        Assert.Contains("All", viewModel.AvailableTargets);
    }

    [Fact]
    public async Task AnalyzeLog_ShouldGenerateEventTable()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var repoRoot = FindRepositoryRoot(currentDir);

        if (repoRoot == null) return;

        var testLogPath = Path.Combine(repoRoot, "data", "sample.log");

        if (!File.Exists(testLogPath)) return;

        viewModel.SelectedLogFile = testLogPath;

        // Act
        await viewModel.AnalyzeLogCommand.ExecuteAsync(null);

        // Assert
        Assert.True(viewModel.HasAnalyzedData);
        Assert.NotNull(viewModel.EventTableRows);
        Assert.NotEmpty(viewModel.EventTableRows);
    }

    [Fact]
    public async Task AnalyzeLog_ShouldGenerateQuickStatsSummary()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var repoRoot = FindRepositoryRoot(currentDir);

        if (repoRoot == null) return;

        var testLogPath = Path.Combine(repoRoot, "data", "sample.log");

        if (!File.Exists(testLogPath)) return;

        viewModel.SelectedLogFile = testLogPath;

        // Act
        await viewModel.AnalyzeLogCommand.ExecuteAsync(null);

        // Assert
        Assert.True(viewModel.HasAnalyzedData);
        Assert.NotNull(viewModel.QuickStatsSummary);
        Assert.NotEmpty(viewModel.QuickStatsSummary);
        Assert.Contains("DMG:", viewModel.QuickStatsSummary);
    }

    [Fact]
    public void EventTypeToggles_CanBeChanged()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.ShowDamageDealt = false;
        viewModel.ShowHealingDone = false;

        // Assert
        Assert.False(viewModel.ShowDamageDealt);
        Assert.False(viewModel.ShowHealingDone);
        Assert.True(viewModel.ShowDamageTaken); // Should remain true
    }

    [Fact]
    public void ChartOptions_CanBeChanged()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.SelectedChartType = "Bar";
        viewModel.SelectedChartInterval = "10s";
        viewModel.ShowDamageTakenOnChart = true;
        viewModel.ShowDpsTrendLine = true;

        // Assert
        Assert.Equal("Bar", viewModel.SelectedChartType);
        Assert.Equal("10s", viewModel.SelectedChartInterval);
        Assert.True(viewModel.ShowDamageTakenOnChart);
        Assert.True(viewModel.ShowDpsTrendLine);
    }

    [Fact]
    public void ComparisonMode_CanBeToggled()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.IsComparisonMode = true;

        // Assert
        Assert.True(viewModel.IsComparisonMode);
    }

    [Fact]
    public void ResetFilters_ShouldResetToDefaults()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.ShowDamageDealt = false;
        viewModel.ShowHealingDone = false;
        viewModel.SelectedDamageType = "Crush";

        // Act
        viewModel.ResetFiltersCommand.Execute(null);

        // Assert
        Assert.True(viewModel.ShowDamageDealt);
        Assert.True(viewModel.ShowHealingDone);
        Assert.Equal("All", viewModel.SelectedDamageType);
    }

    [Fact]
    public void ChartTypes_CollectionShouldContainExpectedOptions()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.Contains("Line", viewModel.ChartTypes);
        Assert.Contains("Bar", viewModel.ChartTypes);
        Assert.Contains("Area", viewModel.ChartTypes);
    }

    [Fact]
    public void ChartIntervals_CollectionShouldContainExpectedOptions()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.Contains("1s", viewModel.ChartIntervals);
        Assert.Contains("5s", viewModel.ChartIntervals);
        Assert.Contains("10s", viewModel.ChartIntervals);
        Assert.Contains("30s", viewModel.ChartIntervals);
        Assert.Contains("1m", viewModel.ChartIntervals);
    }
}
