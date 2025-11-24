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

        // Assert
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
    public async Task AnalyzeLog_WithValidFile_ShouldUpdateStatistics()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        // Navigate from test bin directory to repository root
        var repoRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..");
        var testLogPath = Path.Combine(repoRoot, "data", "sample.log");
        var fullPath = Path.GetFullPath(testLogPath);
        
        // Skip test if sample.log doesn't exist
        if (!File.Exists(fullPath))
        {
            return; // Skip test gracefully
        }
        
        viewModel.SelectedLogFile = fullPath;

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
}
