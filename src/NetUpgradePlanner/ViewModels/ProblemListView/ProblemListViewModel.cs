using System;
using System.Collections.Generic;
using System.Linq;

using NetUpgradePlanner.Mvvm;
using NetUpgradePlanner.Analysis;
using NetUpgradePlanner.Services;

using Terrajobst.ApiCatalog;

namespace NetUpgradePlanner.ViewModels.MainWindow;

internal sealed class ProblemListViewModel : ViewModel
{
    private readonly WorkspaceService _workspaceService;
    private readonly AssemblySelectionService _assemblySelectionService;
    private readonly IconService _iconService;
    private bool _allAssemblies = true;
    private bool _selectedAssemblies;
    private string _filter = string.Empty;
    private IReadOnlyList<ProblemListViewItem> _items = Array.Empty<ProblemListViewItem>();
    private bool _includeMissingFunctionality = true;
    private bool _includeObsoletions = true;
    private bool _includeCrossPlatform = true;
    private bool _includeErrors = true;
    private bool _includeWarnings = true;
    private bool _includeConsistency = true;

    public ProblemListViewModel(WorkspaceService workspaceService,
                                AssemblySelectionService assemblySelectionService,
                                IconService iconService)
    {
        _workspaceService = workspaceService;
        _workspaceService.Changed += WorkspaceService_Changed;

        _assemblySelectionService = assemblySelectionService;
        _assemblySelectionService.Changed += AssemblySelectionService_Changed;

        _iconService = iconService;
    }

    public bool AllAssemblies
    {
        get => _allAssemblies;
        set
        {
            if (_allAssemblies != value)
            {
                _allAssemblies = value;
                OnPropertyChanged();
                UpdateItems();
            }
        }
    }

    public bool SelectedAssemblies
    {
        get => _selectedAssemblies;
        set
        {
            if (_selectedAssemblies != value)
            {
                _selectedAssemblies = value;
                OnPropertyChanged();
                UpdateItems();
            }
        }
    }

    public bool IncludeMissingFunctionality
    {
        get => _includeMissingFunctionality;
        set
        {
            if (_includeMissingFunctionality != value)
            {
                _includeMissingFunctionality = value;
                OnPropertyChanged();
                UpdateItems();
            }
        }
    }

    public bool IncludeObsoletions
    {
        get => _includeObsoletions;
        set
        {
            if (_includeObsoletions != value)
            {
                _includeObsoletions = value;
                OnPropertyChanged();
                UpdateItems();
            }
        }
    }

    public bool IncludeCrossPlatform
    {
        get => _includeCrossPlatform;
        set
        {
            if (_includeCrossPlatform != value)
            {
                _includeCrossPlatform = value;
                OnPropertyChanged();
                UpdateItems();
            }
        }
    }

    public bool IncludeConsistency
    {
        get => _includeConsistency;
        set
        {
            if (_includeConsistency != value)
            {
                _includeConsistency = value;
                OnPropertyChanged();
                UpdateItems();
            }
        }
    }

    public bool IncludeErrors
    {
        get => _includeErrors;
        set
        {
            if (_includeErrors != value)
            {
                _includeErrors = value;
                OnPropertyChanged();
                UpdateItems();
            }
        }
    }

    public bool IncludeWarnings
    {
        get => _includeWarnings;
        set
        {
            if (_includeWarnings != value)
            {
                _includeWarnings = value;
                OnPropertyChanged();
                UpdateItems();
            }
        }
    }

    public string Filter
    {
        get => _filter;
        set
        {
            if (_filter != value)
            {
                _filter = value;
                OnPropertyChanged();
                UpdateItems();
            }
        }
    }

    public IReadOnlyList<ProblemListViewItem> Items
    {
        get => _items;
        set
        {
            if (_items != value)
            {
                _items = value;
                OnPropertyChanged();
            }
        }
    }

    private void WorkspaceService_Changed(object? sender, EventArgs e)
    {
        UpdateItems();
    }

    private void AssemblySelectionService_Changed(object? sender, EventArgs e)
    {
        if (_selectedAssemblies)
            UpdateItems();
    }

    private void UpdateItems()
    {
        var items = ApplyFilter(CreateItems()).ToArray();
        Items = items;
    }

    private IEnumerable<ProblemListViewItem> ApplyFilter(IEnumerable<ProblemListViewItem> items)
    {
        return items.Select(ApplyFilter)
                    .Where(n => n is not null)
                    .Select(n => n!);
    }

    private ProblemListViewItem? ApplyFilter(ProblemListViewItem item)
    {
        if (item.Data is ProblemId problemId)
        {
            if (!_includeMissingFunctionality && problemId.Category == ProblemCategory.MissingFunctionality)
                return null;

            if (!_includeObsoletions && problemId.Category == ProblemCategory.Obsoletion)
                return null;

            if (!_includeCrossPlatform && problemId.Category == ProblemCategory.CrossPlatform)
                return null;

            if (!_includeConsistency && problemId.Category == ProblemCategory.Consistency)
                return null;

            if (!_includeErrors && problemId.Severity == ProblemSeverity.Error)
                return null;

            if (!_includeWarnings && problemId.Severity == ProblemSeverity.Warning)
                return null;
        }

        var matchesFilter = string.IsNullOrEmpty(_filter) ||
                            item.Text.Contains(_filter, StringComparison.OrdinalIgnoreCase);

        if (item.Children.Any())
        {
            if (matchesFilter)
                return item;

            var filteredChildren = ApplyFilter(item.Children).ToArray();
            if (!filteredChildren.Any())
                return null;

            return new ProblemListViewItem(item.Data, item.Icon, item.Text, filteredChildren);
        }
        else if (!matchesFilter)
        {
            return null;
        }
        else
        {
            return item;
        }
    }

    private IReadOnlyList<ProblemListViewItem> CreateItems()
    {
        var report = _workspaceService.Current.Report;
        if (report is null)
            return Array.Empty<ProblemListViewItem>();

        IReadOnlyList<AnalyzedAssembly> analyzedAssemblies;

        if (_allAssemblies)
        {
            analyzedAssemblies = report.AnalyzedAssemblies;
        }
        else
        {
            var selectedAssemblies = _assemblySelectionService.GetSelectedAssemblies().ToHashSet();
            analyzedAssemblies = report.AnalyzedAssemblies.Where(a => selectedAssemblies.Contains(a.Entry)).ToArray();
        }

        if (analyzedAssemblies.Count != 1)
        {
            return analyzedAssemblies.SelectMany(ap => ap.Problems, (ap, p) => (Assembly: ap, Problem: p))
                                     .GroupBy(t => t.Problem.ProblemId)
                                     .OrderBy(g => g.Key.Text)
                                     .Select(g => CreateProblemIdItemForAll(g.Key, g))
                                     .ToArray();
        }
        else
        {
            var analyzedAssembly = analyzedAssemblies.Single();
            return analyzedAssembly.Problems
                                   .GroupBy(a => a.ProblemId)
                                   .OrderBy(g => g.Key.Text)
                                   .Select(CreateProblemIdItemForCurrent)
                                   .ToArray();
        }
    }

    private ProblemListViewItem CreateProblemIdItemForCurrent(IGrouping<ProblemId, Problem> problemGroup)
    {
        var problemId = problemGroup.Key;
        var children = problemGroup.DistinctBy(p => p.Api)
                                   .Select(CreateProblemItem)
                                   .OrderBy(x => x);
        var iconKind = GetIconKind(problemId.Severity);
        var icon = _iconService.GetIcon(iconKind);
        var text = problemId.Text;
        return new ProblemListViewItem(problemId, icon, text, children);
    }

    private ProblemListViewItem CreateProblemIdItemForAll(ProblemId problemId, IEnumerable<(AnalyzedAssembly Assembly, Problem Problem)> problems)
    {
        var children = problems.GroupBy(t => t.Assembly, t => t.Problem)
                               .OrderBy(g => g.Key.Entry.Name)
                               .Select(g => CreateAssemblyItem(g.Key, g));
        var iconKind = GetIconKind(problemId.Severity);
        var icon = _iconService.GetIcon(iconKind);
        var text = problemId.Text;
        return new ProblemListViewItem(problemId, icon, text, children);
    }

    private ProblemListViewItem CreateAssemblyItem(AnalyzedAssembly assembly, IEnumerable<Problem> problems)
    {
        var children = CreateProblemItems(problems);
        var iconKind = IconKind.Assembly;
        var icon = _iconService.GetIcon(iconKind);
        var text = assembly.Entry.Name;
        return new ProblemListViewItem(assembly.Entry, icon, text, children);
    }

    private IEnumerable<ProblemListViewItem> CreateProblemItems(IEnumerable<Problem> problems)
    {
        return problems.DistinctBy(p => p.Api)
                       .Select(CreateProblemItem)
                       .OrderBy(x => x);
    }

    private ProblemListViewItem CreateProblemItem(Problem problem)
    {
        if (problem.Api is not null)
        {
            var api = problem.Api.Value;
            var iconKind = GetIconKind(api.Kind);
            var icon = _iconService.GetIcon(iconKind);
            var text = api.GetFullName();
            return new ProblemListViewItem(problem, icon, text);
        }
        else if (problem.UnresolvedReference is not null)
        {
            var icon = _iconService.GetIcon(IconKind.Assembly);
            var text = problem.UnresolvedReference;
            return new ProblemListViewItem(problem, icon, text);
        }
        else
        {
            throw new Exception("Unexpected problem data");
        }
    }

    private static IconKind GetIconKind(ProblemSeverity severity)
    {
        switch (severity)
        {
            case ProblemSeverity.Warning:
                return IconKind.Warning;
            case ProblemSeverity.Error:
                return IconKind.Error;
            default:
                throw new Exception($"Unexpected severity: {severity}");
        }
    }

    private static IconKind GetIconKind(ApiKind kind)
    {
        switch (kind)
        {
            case ApiKind.Namespace:
                return IconKind.Namespace;
            case ApiKind.Interface:
                return IconKind.Interface;
            case ApiKind.Delegate:
                return IconKind.Delegate;
            case ApiKind.Enum:
                return IconKind.Enum;
            case ApiKind.Struct:
                return IconKind.Struct;
            case ApiKind.Class:
                return IconKind.Class;
            case ApiKind.Constant:
                return IconKind.Constant;
            case ApiKind.EnumItem:
                return IconKind.EnumItem;
            case ApiKind.Field:
                return IconKind.Field;
            case ApiKind.Constructor:
                return IconKind.Method;
            case ApiKind.Destructor:
                return IconKind.Method;
            case ApiKind.Property:
                return IconKind.Property;
            case ApiKind.PropertyGetter:
            case ApiKind.PropertySetter:
                return IconKind.Method;
            case ApiKind.Method:
                return IconKind.Method;
            case ApiKind.Operator:
                return IconKind.Operator;
            case ApiKind.Event:
                return IconKind.Event;
            case ApiKind.EventAdder:
            case ApiKind.EventRemover:
            case ApiKind.EventRaiser:
                return IconKind.Method;
            default:
                throw new Exception($"Unexpected API kind: {kind}");
        }
    }
}
