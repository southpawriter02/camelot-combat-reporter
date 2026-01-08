using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DamageBreakdownChart.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace DamageBreakdownChart.ViewModels;

/// <summary>
/// View model for the pie/donut breakdown visualization.
/// </summary>
public class BreakdownViewModel : INotifyPropertyChanged
{
    private DamageNode? _rootNode;
    private DamageNode? _currentNode;
    private DamageNode? _selectedNode;
    private readonly Stack<DamageNode> _navigationStack = new();

    public BreakdownViewModel(DamageNode? rootNode = null)
    {
        _rootNode = rootNode;
        _currentNode = rootNode;
        UpdateSeries();
    }

    /// <summary>
    /// The pie series for LiveCharts.
    /// </summary>
    public ObservableCollection<ISeries> Series { get; } = new();

    /// <summary>
    /// Breadcrumb path for navigation.
    /// </summary>
    public ObservableCollection<DamageNode> Breadcrumbs { get; } = new();

    /// <summary>
    /// Currently selected node for detail display.
    /// </summary>
    public DamageNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            _selectedNode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectionStats));
        }
    }

    public bool HasSelection => _selectedNode != null;

    /// <summary>
    /// Statistics for the selected node.
    /// </summary>
    public string SelectionStats
    {
        get
        {
            if (_selectedNode == null)
                return string.Empty;

            return $"Damage: {_selectedNode.TotalDamage:N0}\n" +
                   $"Hits: {_selectedNode.HitCount:N0}\n" +
                   $"Avg: {_selectedNode.AverageDamage:F0}\n" +
                   $"Share: {_selectedNode.Percentage:F1}%";
        }
    }

    /// <summary>
    /// Title for current view.
    /// </summary>
    public string CurrentTitle => _currentNode?.Name ?? "All Damage";

    /// <summary>
    /// Updates the root data.
    /// </summary>
    public void UpdateData(DamageNode? rootNode)
    {
        _rootNode = rootNode;
        _currentNode = rootNode;
        _navigationStack.Clear();
        UpdateSeries();
        UpdateBreadcrumbs();
    }

    /// <summary>
    /// Drills down into a node.
    /// </summary>
    public void DrillDown(DamageNode node)
    {
        if (node.Children.Count == 0)
            return;

        if (_currentNode != null)
            _navigationStack.Push(_currentNode);

        _currentNode = node;
        SelectedNode = null;
        UpdateSeries();
        UpdateBreadcrumbs();
    }

    /// <summary>
    /// Navigates up one level.
    /// </summary>
    public void NavigateUp()
    {
        if (_navigationStack.TryPop(out var parent))
        {
            _currentNode = parent;
            SelectedNode = null;
            UpdateSeries();
            UpdateBreadcrumbs();
        }
    }

    public bool CanNavigateUp => _navigationStack.Count > 0;

    private void UpdateSeries()
    {
        Series.Clear();

        if (_currentNode == null || _currentNode.Children.Count == 0)
            return;

        var index = 0;
        foreach (var child in _currentNode.Children.OrderByDescending(c => c.TotalDamage).Take(10))
        {
            var color = ChartColors.GetColor(child, index);
            var series = new PieSeries<double>
            {
                Values = new[] { (double)child.TotalDamage },
                Name = child.Name,
                Fill = new SolidColorPaint(color),
                Pushout = 5,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsFormatter = point => $"{child.Name}\n{child.Percentage:F1}%"
            };

            Series.Add(series);
            index++;
        }

        OnPropertyChanged(nameof(CurrentTitle));
        OnPropertyChanged(nameof(CanNavigateUp));
    }

    private void UpdateBreadcrumbs()
    {
        Breadcrumbs.Clear();

        if (_rootNode != null)
            Breadcrumbs.Add(_rootNode);

        foreach (var node in _navigationStack.Reverse())
        {
            Breadcrumbs.Add(node);
        }

        if (_currentNode != null && _currentNode != _rootNode)
        {
            Breadcrumbs.Add(_currentNode);
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
