using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using NetUpgradePlanner.Services;

using Terrajobst.NetUpgradePlanner;

namespace NetUpgradePlanner.Views;

internal sealed partial class GraphView : UserControl
{
    private readonly WorkspaceService _workspaceService;
    private readonly AssemblyContextMenuService _assemblyContextMenuService;
    private bool _isPanning;
    private Point _origContentMouseDownPoint;
    private MouseButton _mouseButtonDown;

    private GraphDocument _document = GraphDocument.Empty;
    private AssemblySetEntry? _selectedEntry;
    private AssemblySetEntry? _butterflyEntry;
    private NodeHighlighter _nodeHighlighter = new NodeHighlighter();
    private NodeEmphasizer _nodeEmphasizer = new NodeEmphasizer();

    public GraphView(WorkspaceService workspaceService,
                     AssemblyContextMenuService assemblyContextMenuService)
    {
        InitializeComponent();

        _workspaceService = workspaceService;
        _workspaceService.Changed += WorkspaceService_Changed;

        _assemblyContextMenuService = assemblyContextMenuService;
    }

    public AssemblySetEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (_selectedEntry != value)
            {
                _selectedEntry = value;
                UpdateSelectedEntryState();

                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public AssemblySetEntry? ButterflyEntry
    {
        get => _butterflyEntry;
        set
        {
            if (_butterflyEntry != value)
            {
                _butterflyEntry = value;
                GraphChanged();
            }
        }
    }

    public event EventHandler? SelectionChanged;

    private void WorkspaceService_Changed(object? sender, EventArgs e)
    {
        if (_selectedEntry is not null && !_workspaceService.Current.AssemblySet.Entries.Contains(_selectedEntry))
            _selectedEntry = null;

        if (_butterflyEntry is not null && !_workspaceService.Current.AssemblySet.Entries.Contains(_butterflyEntry))
            _butterflyEntry = null;

        GraphChanged();
    }

    private void OnZoomPanMouseDown(object sender, MouseButtonEventArgs e)
    {
        zoomPanControl.Focus();
        Keyboard.Focus(zoomPanControl);
        _mouseButtonDown = e.ChangedButton;
        _origContentMouseDownPoint = e.GetPosition(svgViewer);

        if (_mouseButtonDown == MouseButton.Left)
        {
            // Just a plain old left-down initiates panning mode.
            _isPanning = true;
            // Capture the mouse so that we eventually receive the mouse up event.
            zoomPanControl.CaptureMouse();
            e.Handled = true;
        }

        var canvas = svgViewer.DrawingCanvas;
        var pt = e.GetPosition(canvas);
        var node = _document.Nodes.FirstOrDefault(n => n.Bounds.Contains(pt));
        if (node is not null)
            SelectedEntry = node.Entry;

        if (e.ClickCount > 1)
        {
            if (ButterflyEntry != node?.Entry)
                ButterflyEntry = node?.Entry;
            else
                ButterflyEntry = null;
        }
    }

    private void OnZoomPanMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            zoomPanControl.ReleaseMouseCapture();
            _isPanning = false;
            e.Handled = true;
        }
    }

    private void OnZoomPanMouseMove(object sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            // The user is left-dragging the mouse.
            // Pan the viewport by the appropriate amount.
            var curContentMousePoint = e.GetPosition(svgViewer);
            var dragOffset = curContentMousePoint - _origContentMouseDownPoint;

            zoomPanControl.ContentOffsetX -= dragOffset.X;
            zoomPanControl.ContentOffsetY -= dragOffset.Y;

            e.Handled = true;
        }
    }

    private void OnZoomPanMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var contentZoomCenter = e.GetPosition(svgViewer);
        var wheelMouseDelta = e.Delta;

        // Found the division by 3 gives a little smoothing effect
        var zoomChange = 0.1;
        var zoomFactor = zoomPanControl.ContentScale + zoomChange * wheelMouseDelta / (120 * 3);
        zoomPanControl.ZoomAboutPoint(zoomFactor, contentZoomCenter);

        if (svgViewer.IsKeyboardFocusWithin)
            Keyboard.Focus(zoomPanControl);

        e.Handled = true;
    }

    private async void GraphChanged()
    {
        var workspace = _workspaceService.Current;
        var assemblySet = workspace.AssemblySet;
        var nodeCount = _butterflyEntry is null
            ? assemblySet.Entries.Count
            : assemblySet.Butterfly(_butterflyEntry).Count;

        var isTooLarge = nodeCount > 100;
        if (isTooLarge)
        {
            TooLargeWarning.Visibility = Visibility.Visible;
            canvasScroller.Visibility = Visibility.Collapsed;
            _document = GraphDocument.Empty;
            svgViewer.Load(string.Empty);
            _nodeHighlighter.Clear();
            _nodeEmphasizer.Clear();
            return;
        }
        else
        {
            TooLargeWarning.Visibility = Visibility.Collapsed;
            canvasScroller.Visibility = Visibility.Visible;
        }

        var butterflyEntry = _butterflyEntry;

        var source = await Task.Run(() => CreateSvg(workspace, butterflyEntry));
        await svgViewer.LoadAsync(source);
        _document = GraphDocument.Create(svgViewer.Drawings, assemblySet);

        var report = _workspaceService.Current.Report;
        var problemsByEntry = report is null
                                ? new()
                                : report.AnalyzedAssemblies.ToDictionary(p => p.Entry);

        foreach (var node in _document.Nodes)
        {
            if (problemsByEntry.TryGetValue(node.Entry, out var problems) &&
                problems.Problems.Any())
            {
                var severity = problems.Problems.Max(p => p.ProblemId.Severity);
                if (severity == ProblemSeverity.Warning)
                {
                    node.Box.Brush = Brushes.LemonChiffon;
                }
                else
                {
                    node.Box.Brush = Brushes.LightPink;
                }
            }
            else
            {
                node.Box.Brush = SystemColors.ControlBrush;
            }
        }

        UpdateSelectedEntryState();
        UpdateButterflyEntryState();
    }

    private void UpdateSelectedEntryState()
    {
        var selectedNode = _document.Nodes.FirstOrDefault(n => n.Entry == _selectedEntry);
        _nodeHighlighter.Highlight(selectedNode);
    }

    private void UpdateButterflyEntryState()
    {
        var butterflyNode = _document.Nodes.FirstOrDefault(n => n.Entry == _butterflyEntry);
        _nodeEmphasizer.Emphasize(butterflyNode);
    }

    private static string CreateSvg(Workspace workspace, AssemblySetEntry? butterflyEntry)
    {
        var processDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        var dotPath = Path.Join(processDirectory, "Graphviz", "dot.exe");

        var startInfo = new ProcessStartInfo
        {
            FileName = dotPath,
            Arguments = "-Tsvg",
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        var svgSourceBuilder = new StringBuilder();

        using var dotProcess = new Process()
        {
            StartInfo = startInfo
        };

        dotProcess.OutputDataReceived += (_, e) =>
        {
            svgSourceBuilder.AppendLine(e.Data);
        };

        dotProcess.Start();
        dotProcess.BeginOutputReadLine();

        WriteDotFile(dotProcess.StandardInput, workspace, butterflyEntry);

        try
        {
            dotProcess.WaitForExit();
            if (dotProcess.ExitCode == 0)
                return svgSourceBuilder.ToString();
        }
        catch (Exception)
        {
            // Ignore
        }

        return string.Empty;

        static void WriteDotFile(TextWriter writer, Workspace workspace, AssemblySetEntry? butterflyEntry)
        {
            var assemblySet = workspace.AssemblySet;

            Func<AssemblySetEntry, bool> entryFilter;

            if (butterflyEntry is null)
            {
                entryFilter = _ => true;
            }
            else
            {
                var butterflySet = assemblySet.Butterfly(butterflyEntry);
                entryFilter = e => butterflySet.Contains(e);
            }

            var idByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in assemblySet.Entries.Where(entryFilter))
                idByName.Add(assembly.Name, $"a{idByName.Count}");

            using (var w = new IndentedTextWriter(writer, new string(' ', 4)))
            {
                w.WriteLine("digraph G {");

                w.Indent++;
                w.WriteLine("node [fontname=\"Segoe UI\" shape=\"box\" style=\"rounded\"]");

                foreach (var assembly in assemblySet.Entries.Where(entryFilter))
                {
                    var id = idByName[assembly.Name];
                    var desiredFramework = workspace.AssemblyConfiguration.GetDesiredFramework(assembly);
                    var framework = $"{assembly.TargetFramework} &#x279E; {desiredFramework}";
                    w.WriteLine($"{id} [label=<<b>{assembly.Name}</b><br/>{framework}>]");
                }

                foreach (var assembly in assemblySet.Entries.Where(entryFilter))
                {
                    var source = idByName[assembly.Name];

                    foreach (var dependency in assembly.Dependencies)
                    {
                        if (idByName.TryGetValue(dependency, out var target))
                        {
                            w.WriteLine($"   {source} -> {target}");
                        }
                    }
                }

                w.Indent--;
                w.WriteLine("}");
            }

            writer.Dispose();
        }
    }

    private void GraphContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        _assemblyContextMenuService.Fill(GraphContextMenu.Items);
    }

    internal sealed class GraphDocument
    {
        public static GraphDocument Empty { get; } = new GraphDocument(Array.Empty<GraphNode>());

        public GraphDocument(IEnumerable<GraphNode> nodes)
        {
            Nodes = nodes.ToArray();
        }

        public IReadOnlyList<GraphNode> Nodes { get; }

        public static GraphDocument Create(DrawingGroup? drawing, AssemblySet assemblySet)
        {
            if (drawing is null || assemblySet.IsEmpty)
                return Empty;

            var entryByName = assemblySet.Entries.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
            var minTopLeft = GetMinTopLeft(drawing);
            var nodes = new List<GraphNode>();

            Walk(drawing, minTopLeft, entryByName, nodes);

            return new GraphDocument(nodes);

            static Point GetMinTopLeft(Drawing d)
            {
                var x = d.Bounds.Left;
                var y = d.Bounds.Top;

                if (d is DrawingGroup g)
                {
                    foreach (var c in g.Children)
                    {
                        var cXY = GetMinTopLeft(c);

                        x = Math.Min(x, cXY.X);
                        y = Math.Min(y, cXY.Y);
                    }
                }

                return new Point(x, y);
            }

            static void Walk(Drawing drawing,
                             Point minTopLeft,
                             Dictionary<string, AssemblySetEntry> entryByName,
                             List<GraphNode> nodes)
            {
                if (drawing is DrawingGroup nodeGroup)
                {
                    if (nodeGroup.Children.Count > 1 &&
                        nodeGroup.Children[0] is GeometryDrawing geometry &&
                        nodeGroup.Children[1] is DrawingGroup assemblyGroup)
                    {
                        var offsetBounds = new Rect(drawing.Bounds.Left - minTopLeft.X,
                                                    drawing.Bounds.Top - minTopLeft.Y,
                                                    drawing.Bounds.Width,
                                                    drawing.Bounds.Height);

                        IReadOnlyList<GlyphRunDrawing> assemblyRuns = assemblyGroup.Children.OfType<GlyphRunDrawing>().ToArray();
                        if (assemblyRuns.Any())
                        {
                            var assemblyName = string.Concat(assemblyRuns.SelectMany(ar => ar.GlyphRun.Characters));
                            var entry = entryByName[assemblyName];

                            IReadOnlyList<GlyphRunDrawing> tfmRuns = Array.Empty<GlyphRunDrawing>();

                            if (nodeGroup.Children.Count > 2 &&
                                nodeGroup.Children[2] is DrawingGroup tfmGroup &&
                                tfmGroup.Children.OfType<GlyphRunDrawing>().Any())
                            {
                                tfmRuns = tfmGroup.Children.OfType<GlyphRunDrawing>().ToArray();
                            }

                            var node = new GraphNode(entry, offsetBounds, geometry, assemblyRuns, tfmRuns);
                            nodes.Add(node);
                        }
                    }

                    foreach (var child in nodeGroup.Children)
                        Walk(child, minTopLeft, entryByName, nodes);
                }
            }
        }
    }

    internal sealed class GraphNode
    {
        public GraphNode(AssemblySetEntry entry,
                         Rect bounds,
                         GeometryDrawing box,
                         IReadOnlyList<GlyphRunDrawing> assemblyRun,
                         IReadOnlyList<GlyphRunDrawing> frameworkRuns)
        {
            Entry = entry;
            Bounds = bounds;
            Box = box;
            AssemblyRuns = assemblyRun;
            FrameworkRuns = frameworkRuns;
        }

        public AssemblySetEntry Entry { get; }
        public Rect Bounds { get; }
        public GeometryDrawing Box { get; }
        public IReadOnlyList<GlyphRunDrawing> AssemblyRuns { get; }
        public IReadOnlyList<GlyphRunDrawing> FrameworkRuns { get; }
    }

    internal sealed class NodeHighlighter
    {
        private GraphNode? _highlightedNode;
        private Brush? _oldBoxBrush;
        private Brush? _oldAssemblyBrush;
        private Brush? _oldFrameworkBrush;

        public void Clear()
        {
            _highlightedNode = null;
            _oldBoxBrush = null;
            _oldAssemblyBrush = null;
            _oldFrameworkBrush = null;
        }

        public void Highlight(GraphNode? node)
        {
            if (_highlightedNode is not null)
            {
                _highlightedNode.Box.Brush = _oldBoxBrush;

                foreach (var r in _highlightedNode.AssemblyRuns)
                    r.ForegroundBrush = _oldAssemblyBrush;

                foreach (var r in _highlightedNode.FrameworkRuns)
                    r.ForegroundBrush = _oldFrameworkBrush;
            }

            if (node is null)
            {
                Clear();
            }
            else
            {
                _highlightedNode = node;

                _oldBoxBrush = node.Box.Brush;
                node.Box.Brush = SystemColors.HighlightBrush;

                _oldAssemblyBrush = node.AssemblyRuns.First().ForegroundBrush;
                foreach (var r in node.AssemblyRuns)
                    r.ForegroundBrush = SystemColors.HighlightTextBrush;

                if (node.FrameworkRuns.Any())
                {
                    _oldFrameworkBrush = node.FrameworkRuns.First().ForegroundBrush;
                    foreach (var r in node.FrameworkRuns)
                        r.ForegroundBrush = SystemColors.HighlightTextBrush;
                }
            }
        }
    }

    internal sealed class NodeEmphasizer
    {
        private GraphNode? _emphasizedNode;
        private Pen? _oldBoxPen;

        public void Clear()
        {
            _emphasizedNode = null;
            _oldBoxPen = null;
        }

        public void Emphasize(GraphNode? node)
        {
            if (_emphasizedNode is not null)
                _emphasizedNode.Box.Pen = _oldBoxPen;

            if (node is null)
            {
                Clear();
            }
            else
            {
                _emphasizedNode = node;

                _oldBoxPen = node.Box.Pen;

                node.Box.Pen = new Pen(_emphasizedNode.Box.Pen.Brush, 4.0)
                {
                    Brush = SystemColors.HighlightBrush,
                    DashStyle = DashStyles.Dot
                };
            }
        }
    }
}

