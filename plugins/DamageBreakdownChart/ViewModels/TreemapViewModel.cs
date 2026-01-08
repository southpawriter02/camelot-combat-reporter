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
/// View model for the treemap visualization.
/// </summary>
public class TreemapViewModel : INotifyPropertyChanged
{
    private DamageNode? _rootNode;
    private DamageNode? _currentNode;
    private DamageNode? _selectedNode;
    private readonly Stack<DamageNode> _navigationStack = new();

    public TreemapViewModel(DamageNode? rootNode = null)
    {
        _rootNode = rootNode;
        _currentNode = rootNode;
        UpdateSeries();
    }

    /// <summary>
    /// The pie series for LiveCharts (using pie as treemap alternative).
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
        }
    }

    public bool HasSelection => _selectedNode != null;

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
        UpdateSeries();
        UpdateBreadcrumbs();
    }

    /// <summary>
    /// Navigates back to a specific breadcrumb.
    /// </summary>
    public void NavigateTo(DamageNode node)
    {
        while (_navigationStack.Count > 0)
        {
            var parent = _navigationStack.Pop();
            if (parent.Id == node.Id)
            {
                _currentNode = parent;
                UpdateSeries();
                UpdateBreadcrumbs();
                return;
            }
        }

        // If we didn't find it, go to root
        _currentNode = _rootNode;
        _navigationStack.Clear();
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

        // Using RowSeries as a horizontal bar chart alternative to treemap
        var index = 0;
        foreach (var child in _currentNode.Children.OrderByDescending(c => c.TotalDamage).Take(10))
        {
            var color = ChartColors.GetColor(child, index);
            var series = new RowSeries<double>
            {
                Values = new[] { (double)child.TotalDamage },
                Name = $"{child.Name} ({child.Percentage:F1}%)",
                Fill = new SolidColorPaint(color),
                MaxBarWidth = 40
            };

            Series.Add(series);
            index++;
        }
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

        if (_currentNode != null && _currentNode != _rootNode && !_navigationStack.Contains(_currentNode))
        {
            Breadcrumbs.Add(_currentNode);
        }

        OnPropertyChanged(nameof(CanNavigateUp));
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
