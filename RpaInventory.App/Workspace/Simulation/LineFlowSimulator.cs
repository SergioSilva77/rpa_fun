using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Workspace.Simulation;

public sealed class LineFlowSimulator
{
    private const double SpeedWorldUnitsPerSecond = 80;
    private const int MaxBalls = 2000;
    private const double LineAnchorQuantizeStep = 0.0001;
    private const double ShapeAnchorQuantizeStep = 1;

    private readonly WorkspaceViewModel _workspace;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _lastTick;

    private readonly List<BallState> _balls = new();
    private WorkspaceGraph _graph = WorkspaceGraph.Empty;

    public LineFlowSimulator(WorkspaceViewModel workspace)
    {
        _workspace = workspace;
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16),
        };
        _timer.Tick += Timer_Tick;
    }

    public bool IsRunning => _timer.IsEnabled;

    public void Start()
    {
        Stop(clearBalls: true);

        var starts = _workspace.Shapes.Where(s => s.Kind == WorkspaceShapeKind.Start).ToList();
        if (starts.Count == 0)
            throw new InvalidOperationException("Nenhum item START foi encontrado na área de trabalho.");

        _graph = WorkspaceGraph.Build(_workspace);

        var startEdges = _graph.GetStartEdges(starts);
        if (startEdges.Count == 0)
            throw new InvalidOperationException("O item START não tem nenhuma linha conectada (faça snap das pontas das linhas no START).");

        foreach (var edge in startEdges)
            TrySpawnBall(edge);

        if (_balls.Count == 0)
            throw new InvalidOperationException("Não foi possível iniciar (nenhuma bolinha foi criada).");

        _stopwatch.Restart();
        _lastTick = _stopwatch.Elapsed;
        _timer.Start();
    }

    public void Stop(bool clearBalls)
    {
        if (_timer.IsEnabled)
            _timer.Stop();

        _stopwatch.Stop();
        _graph = WorkspaceGraph.Empty;
        _balls.Clear();

        if (clearBalls)
            _workspace.Balls.Clear();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_balls.Count == 0)
        {
            Stop(clearBalls: true);
            return;
        }

        var now = _stopwatch.Elapsed;
        var dt = now - _lastTick;
        _lastTick = now;

        var seconds = dt.TotalSeconds;
        if (seconds <= 0)
            return;

        var distance = SpeedWorldUnitsPerSecond * seconds;

        var spawns = new List<Edge>();

        for (var i = _balls.Count - 1; i >= 0; i--)
        {
            var ball = _balls[i];
            var completed = AdvanceBall(ball, distance, spawns);
            if (!completed)
                continue;

            _workspace.Balls.Remove(ball.ViewModel);
            _balls.RemoveAt(i);
        }

        if (spawns.Count > 0)
        {
            foreach (var edge in spawns)
                TrySpawnBall(edge);
        }
    }

    private bool AdvanceBall(BallState ball, double distance, List<Edge> spawns)
    {
        var remaining = distance;

        while (remaining > 0)
        {
            var edge = ball.Edge;
            var start = edge.GetPoint(edge.FromT);
            var end = edge.GetPoint(edge.ToT);
            var segment = end - start;
            var length = segment.Length;

            if (length <= double.Epsilon)
            {
                ball.Progress = 1;
            }
            else
            {
                var remainingOnSegment = (1 - ball.Progress) * length;
                if (remaining < remainingOnSegment)
                {
                    ball.Progress += remaining / length;
                    remaining = 0;
                }
                else
                {
                    ball.Progress = 1;
                    remaining -= remainingOnSegment;
                }
            }

            var pos = edge.GetPoint(edge.FromT + ((edge.ToT - edge.FromT) * ball.Progress));
            ball.ViewModel.X = pos.X;
            ball.ViewModel.Y = pos.Y;

            if (ball.Progress < 1)
                return false;

            var nextEdges = _graph
                .GetOutgoingEdges(edge.To)
                .Where(e => !Equals(e.To, edge.From))
                .ToList();
            if (nextEdges.Count == 0)
                return true;

            var keep = nextEdges[0];
            for (var i = 1; i < nextEdges.Count; i++)
                spawns.Add(nextEdges[i]);

            ball.Edge = keep;
            ball.Progress = 0;
        }

        return false;
    }

    private void TrySpawnBall(Edge edge)
    {
        if (_balls.Count >= MaxBalls)
            return;

        var start = edge.GetPoint(edge.FromT);
        var vm = new WorkspaceBallViewModel(start.X, start.Y);
        _workspace.Balls.Add(vm);
        _balls.Add(new BallState(vm, edge));
    }

    private sealed class BallState
    {
        public BallState(WorkspaceBallViewModel viewModel, Edge edge)
        {
            ViewModel = viewModel;
            Edge = edge;
        }

        public WorkspaceBallViewModel ViewModel { get; }
        public Edge Edge { get; set; }
        public double Progress { get; set; }
    }

    private abstract record NodeKey;

    private sealed record FreePointNodeKey(FreeWorkspacePoint Point) : NodeKey;
    private sealed record LineAnchorNodeKey(LineViewModel Line, double T) : NodeKey;
    private sealed record ShapeAnchorNodeKey(IWorkspaceSurface Shape, double LocalX, double LocalY) : NodeKey;

    private sealed record LineNode(double T, NodeKey Key);

    private sealed record Edge(NodeKey From, NodeKey To, LineViewModel Line, double FromT, double ToT)
    {
        public Point GetPoint(double t)
        {
            var p1 = new Point(Line.P1.X, Line.P1.Y);
            var p2 = new Point(Line.P2.X, Line.P2.Y);
            return p1 + ((p2 - p1) * t);
        }
    }

    private sealed class WorkspaceGraph
    {
        private readonly Dictionary<NodeKey, List<Edge>> _outgoing;
        private readonly Dictionary<IWorkspaceSurface, HashSet<NodeKey>> _shapeNodes;

        private WorkspaceGraph(Dictionary<NodeKey, List<Edge>> outgoing, Dictionary<IWorkspaceSurface, HashSet<NodeKey>> shapeNodes)
        {
            _outgoing = outgoing;
            _shapeNodes = shapeNodes;
        }

        public static WorkspaceGraph Empty { get; } = new(outgoing: new Dictionary<NodeKey, List<Edge>>(), shapeNodes: new Dictionary<IWorkspaceSurface, HashSet<NodeKey>>());

        public static WorkspaceGraph Build(WorkspaceViewModel workspace)
        {
            var lineNodes = new Dictionary<LineViewModel, List<LineNode>>();
            var outgoing = new Dictionary<NodeKey, List<Edge>>();
            var shapeNodes = new Dictionary<IWorkspaceSurface, HashSet<NodeKey>>();

            foreach (var line in workspace.Lines)
            {
                AddLineNode(line, t: 0, GetNodeKey(line.P1));
                AddLineNode(line, t: 1, GetNodeKey(line.P2));
            }

            foreach (var line in workspace.Lines)
            {
                CollectExternalAnchors(line.P1);
                CollectExternalAnchors(line.P2);
            }

            foreach (var (line, nodes) in lineNodes)
            {
                var ordered = nodes
                    .OrderBy(n => n.T)
                    .GroupBy(n => n.Key)
                    .Select(g => g.OrderBy(n => n.T).First())
                    .OrderBy(n => n.T)
                    .ToList();

                for (var i = 0; i < ordered.Count - 1; i++)
                {
                    var a = ordered[i];
                    var b = ordered[i + 1];

                    if (Equals(a.Key, b.Key))
                        continue;

                    AddEdge(new Edge(a.Key, b.Key, line, a.T, b.T));
                    AddEdge(new Edge(b.Key, a.Key, line, b.T, a.T));
                }
            }

            return new WorkspaceGraph(outgoing, shapeNodes);

            void AddLineNode(LineViewModel line, double t, NodeKey key)
            {
                if (!lineNodes.TryGetValue(line, out var list))
                {
                    list = new List<LineNode>();
                    lineNodes[line] = list;
                }

                list.Add(new LineNode(Math.Clamp(t, 0, 1), key));
            }

            void AddEdge(Edge edge)
            {
                if (!outgoing.TryGetValue(edge.From, out var list))
                {
                    list = new List<Edge>();
                    outgoing[edge.From] = list;
                }

                list.Add(edge);
            }

            void CollectExternalAnchors(IMovableWorkspacePoint point)
            {
                if (point is PointOnLineWorkspacePoint onLine)
                    AddLineNode(onLine.ParentLine, Quantize(onLine.T, LineAnchorQuantizeStep), GetNodeKey(point));

                if (point is PointOnShapeWorkspacePoint onShape)
                {
                    var key = GetNodeKey(point);
                    if (!shapeNodes.TryGetValue(onShape.Shape, out var set))
                    {
                        set = new HashSet<NodeKey>();
                        shapeNodes[onShape.Shape] = set;
                    }

                    set.Add(key);
                }
            }
        }

        public IReadOnlyList<Edge> GetStartEdges(IReadOnlyList<WorkspaceShapeViewModel> startShapes)
        {
            var edges = new HashSet<Edge>();

            foreach (var start in startShapes)
            {
                if (!_shapeNodes.TryGetValue(start, out var nodes))
                    continue;

                foreach (var node in nodes)
                {
                    foreach (var e in GetOutgoingEdges(node))
                        edges.Add(e);
                }
            }

            return edges.ToList();
        }

        public IReadOnlyList<Edge> GetOutgoingEdges(NodeKey node)
        {
            if (node is ShapeAnchorNodeKey shapeKey && _shapeNodes.TryGetValue(shapeKey.Shape, out var nodes))
            {
                var edges = new HashSet<Edge>();
                foreach (var n in nodes)
                {
                    if (_outgoing.TryGetValue(n, out var list))
                    {
                        foreach (var e in list)
                            edges.Add(e);
                    }
                }

                return edges.ToList();
            }

            return _outgoing.TryGetValue(node, out var outgoing) ? outgoing : Array.Empty<Edge>();
        }
    }

    private static NodeKey GetNodeKey(IMovableWorkspacePoint point)
        => point switch
        {
            FreeWorkspacePoint free => new FreePointNodeKey(free),
            PointOnLineWorkspacePoint onLine => new LineAnchorNodeKey(onLine.ParentLine, Quantize(onLine.T, LineAnchorQuantizeStep)),
            PointOnShapeWorkspacePoint onShape => new ShapeAnchorNodeKey(onShape.Shape, Quantize(onShape.LocalX, ShapeAnchorQuantizeStep), Quantize(onShape.LocalY, ShapeAnchorQuantizeStep)),
            _ => new FreePointNodeKey(new FreeWorkspacePoint(point.X, point.Y)),
        };

    private static double Quantize(double value, double step)
    {
        if (step <= 0)
            return value;

        return Math.Round(value / step) * step;
    }
}
