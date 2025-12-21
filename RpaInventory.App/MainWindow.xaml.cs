using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using RpaInventory.App.Inventory.Items;
using RpaInventory.App.Inventory.ViewModels;
using RpaInventory.App.Settings;
using RpaInventory.App.Workspace.Geometry;
using RpaInventory.App.Workspace.Simulation;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App;

public partial class MainWindow : Window, IExecutionContext
{
    private const string DragDataFormat = "RpaInventory.InventoryItem";
    private const double DragStartThresholdPixels = 4;
    private const double SnapThresholdPixels = 14;
    private const double MinZoom = 0.01;
    private const double MaxZoom = 400;

    private Point _slotDragStart;
    private SlotViewModel? _slotDragSource;
    private SlotViewModel? _hoveredSlot;
    private string? _hoveredSlotTag;

    private bool _isDraftingLine;
    private Point _draftStartWorld;
    private SnapCandidate? _draftStartSnapCandidate;
    private SnapCandidate? _draftEndSnapCandidate;
    private WorkspaceShapeViewModel? _draftLogicDecisionSource;
    private LogicBranchKind _draftLogicDecisionBranch = LogicBranchKind.None;

    private bool _isSelecting;
    private Point _selectionStartWorld;

    private bool _isDraggingSelection;
    private bool _selectionDragAllowSnap;
    private LineViewModel? _selectionDragPrimaryLine;
    private Point _lastSelectionDragWorld;
    private SnapCandidate? _selectionDragSnapCandidate;
    private EndpointKind _selectionDragSnapEndpoint;

    private bool _isDraggingEndpoint;
    private LineViewModel? _endpointDragLine;
    private EndpointKind _endpointDragEndpoint;
    private SnapCandidate? _endpointDragSnapCandidate;

    private Point _lastWorkspaceMouseWorld;
    private int _nextZIndex = 100;
    private bool _isUpdatingScrollbars;
    private LineFlowSimulator? _lineFlowSimulator;

    private bool _isSpaceDown;
    private bool _isPanning;
    private Point _panStartViewport;

    private IInventoryItem? _selectedBackpackItem;
    private SettingsViewModel _settings = SettingsViewModel.Default;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            WorkspaceCanvas.Focus();
            UpdateScrollbars();
        };

        WorkspaceCanvas.SizeChanged += (_, _) => UpdateScrollbars();
    }

    protected override void OnClosed(EventArgs e)
    {
        _lineFlowSimulator?.Stop(clearBalls: true);
        base.OnClosed(e);
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;
    private WorkspaceViewModel? Workspace => ViewModel?.Workspace;

    public void ShowInfo(string title, string message)
        => MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string title, string message)
        => MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    private static bool IsCtrlDown()
        => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

    private static bool IsShiftDown()
        => Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

    private static bool IsAltDown()
        => Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && !e.IsRepeat)
        {
            _isSpaceDown = true;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.E && !e.IsRepeat)
        {
            ViewModel?.ToggleInventory();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete && !e.IsRepeat)
        {
            DeleteSelectedWorkspaceItems();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && !e.IsRepeat)
        {
            if (_selectedBackpackItem is not null)
            {
                _selectedBackpackItem = null;
                WorkspaceCanvas.Cursor = Cursors.Arrow;
                e.Handled = true;
                return;
            }
        }

        if (TryHandleBackpackSelection(e))
        {
            e.Handled = true;
            return;
        }

        if (TryHandleHotbarAssign(e))
            return;
    }

    private bool TryHandleBackpackSelection(KeyEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel is null || viewModel.IsInventoryOpen)
            return false;

        var slotIndex = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            Key.D4 or Key.NumPad4 => 3,
            Key.D5 or Key.NumPad5 => 4,
            Key.D6 or Key.NumPad6 => 5,
            Key.D7 or Key.NumPad7 => 6,
            _ => -1,
        };

        if (slotIndex < 0 || slotIndex >= viewModel.BackpackSlots.Count)
            return false;

        var slot = viewModel.BackpackSlots[slotIndex];
        _selectedBackpackItem = slot.Item;

        if (_selectedBackpackItem is not null)
        {
            WorkspaceCanvas.Cursor = Cursors.Cross;
        }
        else
        {
            _selectedBackpackItem = null;
            WorkspaceCanvas.Cursor = Cursors.Arrow;
        }

        return true;
    }

    private bool TryHandleHotbarAssign(KeyEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel is null || !viewModel.IsInventoryOpen)
            return false;

        var slotIndex = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            Key.D4 or Key.NumPad4 => 3,
            Key.D5 or Key.NumPad5 => 4,
            Key.D6 or Key.NumPad6 => 5,
            Key.D7 or Key.NumPad7 => 6,
            _ => -1,
        };

        if (slotIndex < 0)
            return false;

        if (_hoveredSlotTag != "InventorySlot" || _hoveredSlot?.Item is null)
            return false;

        viewModel.BackpackSlots[slotIndex].Item = _hoveredSlot.Item;
        e.Handled = true;
        return true;
    }

    private void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _isSpaceDown = false;
            if (_isPanning)
            {
                _isPanning = false;
                WorkspaceCanvas.ReleaseMouseCapture();
                WorkspaceCanvas.Cursor = Cursors.Arrow;
            }
            e.Handled = true;
            return;
        }

        if (!_isDraftingLine)
            return;

        if (e.Key is Key.LeftShift or Key.RightShift)
        {
            FinalizeDraftLine(_lastWorkspaceMouseWorld);
            WorkspaceCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (Workspace is null)
            return;

        _lineFlowSimulator ??= new LineFlowSimulator(Workspace);

        try
        {
            _lineFlowSimulator.Start();
        }
        catch (Exception ex)
        {
            ShowError("START", ex.Message);
        }
    }

    private void Slot_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        if (element.DataContext is not SlotViewModel slot || slot.Item is null)
            return;

        _slotDragSource = slot;
        _slotDragStart = e.GetPosition(this);
    }

    private void Slot_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        if (_slotDragSource?.Item is null)
            return;

        var current = e.GetPosition(this);
        if (!IsBeyondDragThreshold(_slotDragStart, current))
            return;

        var item = _slotDragSource.Item;

        var data = new DataObject();
        data.SetData(DragDataFormat, item);
        data.SetData(typeof(IInventoryItem), item);

        DragDrop.DoDragDrop((DependencyObject)sender as UIElement ?? this, data, DragDropEffects.Copy);

        _slotDragSource = null;
    }

    private void Slot_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        if (element.DataContext is not SlotViewModel slot)
            return;

        _hoveredSlot = slot;
        _hoveredSlotTag = element.Tag as string;
    }

    private void Slot_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        if (element.DataContext is not SlotViewModel slot)
            return;

        if (!ReferenceEquals(_hoveredSlot, slot))
            return;

        _hoveredSlot = null;
        _hoveredSlotTag = null;
    }

    private static bool IsBeyondDragThreshold(Point start, Point current)
        => Math.Abs(current.X - start.X) >= DragStartThresholdPixels
           || Math.Abs(current.Y - start.Y) >= DragStartThresholdPixels;

    private void BackpackSlot_DragEnter(object sender, DragEventArgs e)
        => SetDragFeedback(e, allowDrop: TryGetDragItem(e.Data, out _));

    private void BackpackSlot_DragOver(object sender, DragEventArgs e)
        => SetDragFeedback(e, allowDrop: TryGetDragItem(e.Data, out _));

    private void BackpackSlot_Drop(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        if (element.DataContext is not SlotViewModel slot)
            return;

        if (!TryGetDragItem(e.Data, out var item) || item is null)
            return;

        slot.Item = item;
        e.Handled = true;
    }

    private void Workspace_DragEnter(object sender, DragEventArgs e)
        => SetDragFeedback(e, allowDrop: TryGetDragItem(e.Data, out _));

    private void Workspace_DragOver(object sender, DragEventArgs e)
        => SetDragFeedback(e, allowDrop: TryGetDragItem(e.Data, out _));

    private void Workspace_Drop(object sender, DragEventArgs e)
    {
        if (Workspace is null || ViewModel is null)
            return;

        if (!TryGetDragItem(e.Data, out var item) || item is null)
            return;

        var dropViewport = e.GetPosition(WorkspaceCanvas);
        var dropWorld = ViewportToWorld(dropViewport);

        PlaceItemOnWorkspace(item, dropWorld);
        e.Handled = true;
    }

    private void PlaceItemOnWorkspace(IInventoryItem item, Point worldPoint)
    {
        if (Workspace is null || ViewModel is null)
            return;

        if (item is IWorkspacePlaceableInventoryItem placeable)
            placeable.PlaceOnWorkspace(Workspace, worldPoint);
        else
            ViewModel.AddToWorkspace(item);

        UpdateScrollbars();
    }

    private void PlaceSelectedBackpackItem(Point worldPoint)
    {
        if (_selectedBackpackItem is null)
            return;

        PlaceItemOnWorkspace(_selectedBackpackItem, worldPoint);
        _selectedBackpackItem = null;
        WorkspaceCanvas.Cursor = Cursors.Arrow;
    }

    private static void SetDragFeedback(DragEventArgs e, bool allowDrop)
    {
        e.Effects = allowDrop ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private static bool TryGetDragItem(IDataObject data, out IInventoryItem? item)
    {
        if (data.GetDataPresent(DragDataFormat))
        {
            item = data.GetData(DragDataFormat) as IInventoryItem;
            return item is not null;
        }

        if (data.GetDataPresent(typeof(IInventoryItem)))
        {
            item = data.GetData(typeof(IInventoryItem)) as IInventoryItem;
            return item is not null;
        }

        item = null;
        return false;
    }

    private void WorkspaceCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!IsCtrlDown())
            return;

        var zoomFactor = e.Delta > 0 ? 1.1 : (1 / 1.1);
        var steps = Math.Abs(e.Delta) / 120.0;
        zoomFactor = Math.Pow(zoomFactor, steps);

        var mouseViewport = e.GetPosition(WorkspaceCanvas);
        ZoomAt(mouseViewport, zoomFactor);

        e.Handled = true;
    }

    private void ZoomAt(Point viewportPoint, double zoomFactor)
    {
        var matrix = WorldTransform.Matrix;
        var scale = GetWorldScale(matrix);

        var desiredScale = Math.Clamp(scale * zoomFactor, MinZoom, MaxZoom);
        zoomFactor = desiredScale / scale;

        matrix.Translate(-viewportPoint.X, -viewportPoint.Y);
        matrix.Scale(zoomFactor, zoomFactor);
        matrix.Translate(viewportPoint.X, viewportPoint.Y);
        WorldTransform.Matrix = matrix;

        UpdateScrollbars();
    }

    private static double GetWorldScale(Matrix matrix)
    {
        var sx = Math.Sqrt((matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12));
        return sx <= double.Epsilon ? 1 : sx;
    }

    private Point ViewportToWorld(Point viewportPoint)
    {
        var matrix = WorldTransform.Matrix;
        if (!matrix.HasInverse)
            return viewportPoint;

        matrix.Invert();
        return matrix.Transform(viewportPoint);
    }

    private void WorkspaceHScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingScrollbars)
            return;

        var currentTopLeft = ViewportToWorld(new Point(0, 0));
        SetPan(worldX0: e.NewValue, worldY0: currentTopLeft.Y);
    }

    private void WorkspaceVScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingScrollbars)
            return;

        var currentTopLeft = ViewportToWorld(new Point(0, 0));
        SetPan(worldX0: currentTopLeft.X, worldY0: e.NewValue);
    }

    private void SetPan(double worldX0, double worldY0)
    {
        var matrix = WorldTransform.Matrix;
        var scale = GetWorldScale(matrix);

        matrix.OffsetX = -worldX0 * scale;
        matrix.OffsetY = -worldY0 * scale;
        WorldTransform.Matrix = matrix;

        UpdateScrollbars();
    }

    private void StartPan(Point viewportPoint)
    {
        _isPanning = true;
        _panStartViewport = viewportPoint;
        WorkspaceCanvas.Cursor = Cursors.Hand;
    }

    private void UpdatePan(Point currentViewportPoint)
    {
        if (!_isPanning)
            return;

        var deltaViewport = currentViewportPoint - _panStartViewport;
        var matrix = WorldTransform.Matrix;
        var scale = GetWorldScale(matrix);

        var currentTopLeft = ViewportToWorld(new Point(0, 0));
        var deltaWorld = new Vector(deltaViewport.X / scale, deltaViewport.Y / scale);

        SetPan(worldX0: currentTopLeft.X - deltaWorld.X, worldY0: currentTopLeft.Y - deltaWorld.Y);

        _panStartViewport = currentViewportPoint;
    }

    private void UpdateScrollbars()
    {
        if (Workspace is null)
            return;

        var viewportWidth = WorkspaceCanvas.ActualWidth;
        var viewportHeight = WorkspaceCanvas.ActualHeight;
        if (viewportWidth <= 1 || viewportHeight <= 1)
            return;

        if (!TryGetWorkspaceContentBounds(out var contentBounds))
        {
            _isUpdatingScrollbars = true;
            WorkspaceHScrollBar.Visibility = Visibility.Collapsed;
            WorkspaceVScrollBar.Visibility = Visibility.Collapsed;
            _isUpdatingScrollbars = false;
            return;
        }

        contentBounds.Inflate(40, 40);

        var matrix = WorldTransform.Matrix;
        var scale = GetWorldScale(matrix);

        var viewTopLeft = ViewportToWorld(new Point(0, 0));
        var viewWidthWorld = viewportWidth / scale;
        var viewHeightWorld = viewportHeight / scale;
        var viewRect = new Rect(viewTopLeft.X, viewTopLeft.Y, viewWidthWorld, viewHeightWorld);

        var epsilon = 0.1;
        var needsH = viewRect.Left > contentBounds.Left + epsilon || viewRect.Right < contentBounds.Right - epsilon;
        var needsV = viewRect.Top > contentBounds.Top + epsilon || viewRect.Bottom < contentBounds.Bottom - epsilon;

        _isUpdatingScrollbars = true;

        if (needsH)
        {
            var min = contentBounds.Left;
            var max = contentBounds.Right - viewWidthWorld;
            if (max < min)
                (min, max) = (max, min);

            WorkspaceHScrollBar.Minimum = min;
            WorkspaceHScrollBar.Maximum = max;
            WorkspaceHScrollBar.ViewportSize = viewWidthWorld;
            WorkspaceHScrollBar.SmallChange = Math.Max(5, viewWidthWorld * 0.05);
            WorkspaceHScrollBar.LargeChange = Math.Max(20, viewWidthWorld * 0.2);
            WorkspaceHScrollBar.Value = Math.Clamp(viewTopLeft.X, min, max);
            WorkspaceHScrollBar.Visibility = Visibility.Visible;
        }
        else
        {
            WorkspaceHScrollBar.Visibility = Visibility.Collapsed;
        }

        if (needsV)
        {
            var min = contentBounds.Top;
            var max = contentBounds.Bottom - viewHeightWorld;
            if (max < min)
                (min, max) = (max, min);

            WorkspaceVScrollBar.Minimum = min;
            WorkspaceVScrollBar.Maximum = max;
            WorkspaceVScrollBar.ViewportSize = viewHeightWorld;
            WorkspaceVScrollBar.SmallChange = Math.Max(5, viewHeightWorld * 0.05);
            WorkspaceVScrollBar.LargeChange = Math.Max(20, viewHeightWorld * 0.2);
            WorkspaceVScrollBar.Value = Math.Clamp(viewTopLeft.Y, min, max);
            WorkspaceVScrollBar.Visibility = Visibility.Visible;
        }
        else
        {
            WorkspaceVScrollBar.Visibility = Visibility.Collapsed;
        }

        _isUpdatingScrollbars = false;

        var clampedX = WorkspaceHScrollBar.Visibility == Visibility.Visible ? WorkspaceHScrollBar.Value : viewTopLeft.X;
        var clampedY = WorkspaceVScrollBar.Visibility == Visibility.Visible ? WorkspaceVScrollBar.Value : viewTopLeft.Y;

        if (Math.Abs(clampedX - viewTopLeft.X) > epsilon || Math.Abs(clampedY - viewTopLeft.Y) > epsilon)
        {
            matrix.OffsetX = -clampedX * scale;
            matrix.OffsetY = -clampedY * scale;
            WorldTransform.Matrix = matrix;
        }
    }

    private bool TryGetWorkspaceContentBounds(out Rect bounds)
    {
        bounds = default;

        if (Workspace is null)
            return false;

        var hasAny = false;
        var minX = 0d;
        var minY = 0d;
        var maxX = 0d;
        var maxY = 0d;

        void IncludePoint(double x, double y)
        {
            if (!hasAny)
            {
                hasAny = true;
                minX = maxX = x;
                minY = maxY = y;
                return;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        void IncludeRect(Rect rect)
        {
            IncludePoint(rect.Left, rect.Top);
            IncludePoint(rect.Right, rect.Bottom);
        }

        foreach (var shape in Workspace.Shapes)
            IncludeRect(shape.Bounds);

        foreach (var image in Workspace.Images)
            IncludeRect(image.Bounds);

        foreach (var line in Workspace.Lines)
        {
            IncludePoint(line.P1.X, line.P1.Y);
            IncludePoint(line.P2.X, line.P2.Y);
        }

        if (!hasAny)
            return false;

        bounds = new Rect(new Point(minX, minY), new Point(maxX, maxY));
        return true;
    }

    private void WorkspaceCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Workspace is null)
            return;

        WorkspaceCanvas.Focus();
        _lastWorkspaceMouseWorld = ViewportToWorld(e.GetPosition(WorkspaceCanvas));

        if (_isSpaceDown)
        {
            StartPan(e.GetPosition(WorkspaceCanvas));
            WorkspaceCanvas.CaptureMouse();
            e.Handled = true;
            return;
        }

        if (_selectedBackpackItem is not null && IsWorkspaceBackgroundClick(e))
        {
            PlaceSelectedBackpackItem(_lastWorkspaceMouseWorld);
            e.Handled = true;
            return;
        }

        if (!IsWorkspaceBackgroundClick(e))
            return;

        if (IsShiftDown())
        {
            StartDraftLine(_lastWorkspaceMouseWorld);
            WorkspaceCanvas.CaptureMouse();
            e.Handled = true;
            return;
        }

        StartSelection(_lastWorkspaceMouseWorld, additive: IsCtrlDown());
        WorkspaceCanvas.CaptureMouse();
        e.Handled = true;
    }

    private static bool IsWorkspaceBackgroundClick(MouseButtonEventArgs e)
        => ReferenceEquals(e.OriginalSource, e.Source) || e.OriginalSource is Canvas;

    private void WorkspaceCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        _lastWorkspaceMouseWorld = ViewportToWorld(e.GetPosition(WorkspaceCanvas));

        if (_isPanning)
        {
            UpdatePan(e.GetPosition(WorkspaceCanvas));
            e.Handled = true;
            return;
        }

        if (_isDraftingLine)
        {
            UpdateDraftLine(_lastWorkspaceMouseWorld);
            e.Handled = true;
            return;
        }

        if (_isSelecting)
        {
            UpdateSelectionRect(_lastWorkspaceMouseWorld);
            e.Handled = true;
            return;
        }

        if (e.LeftButton != MouseButtonState.Released || _isDraggingSelection || _isDraggingEndpoint)
            return;

        if (IsShiftDown())
            ShowSnapPreview(FindBestSnapCandidate(_lastWorkspaceMouseWorld, excludeLine: null, excludePoint: null));
        else
            ShowSnapPreview(null);
    }

    private void WorkspaceCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            WorkspaceCanvas.ReleaseMouseCapture();
            WorkspaceCanvas.Cursor = Cursors.Arrow;
            e.Handled = true;
            return;
        }

        if (_isDraftingLine)
        {
            FinalizeDraftLine(_lastWorkspaceMouseWorld);
            WorkspaceCanvas.ReleaseMouseCapture();
            e.Handled = true;
            return;
        }

        if (_isSelecting)
        {
            FinalizeSelection(_lastWorkspaceMouseWorld, additive: IsCtrlDown());
            WorkspaceCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void StartDraftLine(Point startWorld)
    {
        if (Workspace is null)
            return;

        _isDraftingLine = true;

        _draftStartSnapCandidate = FindBestSnapCandidate(startWorld, excludeLine: null, excludePoint: null);
        _draftStartWorld = _draftStartSnapCandidate?.WorldPoint ?? startWorld;
        _draftEndSnapCandidate = null;

        Workspace.DraftLine.IsActive = true;
        Workspace.DraftLine.X1 = _draftStartWorld.X;
        Workspace.DraftLine.Y1 = _draftStartWorld.Y;
        Workspace.DraftLine.X2 = _draftStartWorld.X;
        Workspace.DraftLine.Y2 = _draftStartWorld.Y;

        ConfigureDraftLogicBranch();
        Workspace.SnapPreview.Hide();
    }

    private void UpdateDraftLine(Point endWorld)
    {
        if (Workspace is null)
            return;

        Workspace.DraftLine.X2 = endWorld.X;
        Workspace.DraftLine.Y2 = endWorld.Y;

        _draftEndSnapCandidate = FindBestSnapCandidate(endWorld, excludeLine: null, excludePoint: null);
        ShowSnapPreview(_draftEndSnapCandidate);
    }

    private void FinalizeDraftLine(Point endWorld)
    {
        if (Workspace is null)
            return;

        if (!_isDraftingLine)
            return;

        _isDraftingLine = false;
        Workspace.DraftLine.IsActive = false;

        var p1 = _draftStartSnapCandidate?.CreatePoint() ?? new FreeWorkspacePoint(_draftStartWorld.X, _draftStartWorld.Y);
        var p2 = _draftEndSnapCandidate?.CreatePoint() ?? new FreeWorkspacePoint(endWorld.X, endWorld.Y);

        var line = new LineViewModel(p1, p2);
        Workspace.Lines.Add(line);
        FinalizeDraftLogicBranch(line);

        Workspace.DraftLine.BranchKind = LogicBranchKind.None;
        _draftLogicDecisionSource = null;
        _draftLogicDecisionBranch = LogicBranchKind.None;
        _draftStartSnapCandidate = null;
        _draftEndSnapCandidate = null;
        Workspace.SnapPreview.Hide();

        UpdateScrollbars();
    }

    private void ConfigureDraftLogicBranch()
    {
        if (Workspace is null)
            return;

        _draftLogicDecisionSource = TryGetLogicDecisionSource(_draftStartSnapCandidate);
        _draftLogicDecisionBranch = _draftLogicDecisionSource is null ? LogicBranchKind.None : GetNextDecisionBranch(_draftLogicDecisionSource);
        Workspace.DraftLine.BranchKind = _draftLogicDecisionBranch;
    }

    private void FinalizeDraftLogicBranch(LineViewModel line)
    {
        if (Workspace is null || _draftLogicDecisionSource is null)
            return;

        if (!IsLineConnectedToSurface(line, _draftLogicDecisionSource))
            return;

        if (_draftLogicDecisionBranch is LogicBranchKind.True or LogicBranchKind.False)
        {
            AssignDecisionBranch(_draftLogicDecisionSource, line, _draftLogicDecisionBranch);
            return;
        }

        ShowDecisionBranchPicker(_draftLogicDecisionSource, line);
    }

    private LogicBranchKind GetNextDecisionBranch(WorkspaceShapeViewModel decision)
    {
        if (Workspace is null)
            return LogicBranchKind.None;

        var hasTrue = Workspace.Lines.Any(l => l.BranchKind == LogicBranchKind.True && IsLineConnectedToSurface(l, decision));
        var hasFalse = Workspace.Lines.Any(l => l.BranchKind == LogicBranchKind.False && IsLineConnectedToSurface(l, decision));

        if (!hasTrue)
            return LogicBranchKind.True;

        if (!hasFalse)
            return LogicBranchKind.False;

        return LogicBranchKind.None;
    }

    private void AssignDecisionBranch(WorkspaceShapeViewModel decision, LineViewModel line, LogicBranchKind branch)
    {
        if (Workspace is null)
            return;

        var toRemove = Workspace.Lines
            .Where(existing => !ReferenceEquals(existing, line))
            .Where(existing => existing.BranchKind == branch && IsLineConnectedToSurface(existing, decision))
            .ToList();

        foreach (var existing in toRemove)
            RemoveLine(existing);

        line.BranchKind = branch;
    }

    private void ShowDecisionBranchPicker(WorkspaceShapeViewModel decision, LineViewModel line)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = WorkspaceCanvas,
            Placement = PlacementMode.MousePoint,
            VerticalOffset = 14,
        };

        var trueItem = new MenuItem { Header = "Condição Verdadeira" };
        trueItem.Click += (_, _) => AssignDecisionBranch(decision, line, LogicBranchKind.True);

        var falseItem = new MenuItem { Header = "Condição Falsa" };
        falseItem.Click += (_, _) => AssignDecisionBranch(decision, line, LogicBranchKind.False);

        menu.Items.Add(trueItem);
        menu.Items.Add(falseItem);
        menu.IsOpen = true;
    }

    private static WorkspaceShapeViewModel? TryGetLogicDecisionSource(SnapCandidate? candidate)
        => candidate switch
        {
            PointOnShapeCandidate { Shape: WorkspaceShapeViewModel { Kind: WorkspaceShapeKind.LogicDecision } shape } => shape,
            ExistingPointCandidate { Point: PointOnShapeWorkspacePoint { Shape: WorkspaceShapeViewModel { Kind: WorkspaceShapeKind.LogicDecision } shape } } => shape,
            _ => null,
        };

    private static bool IsLineConnectedToSurface(LineViewModel line, IWorkspaceSurface surface)
        => line.P1 is PointOnShapeWorkspacePoint p1 && ReferenceEquals(p1.Shape, surface)
           || line.P2 is PointOnShapeWorkspacePoint p2 && ReferenceEquals(p2.Shape, surface);

    private void RemoveLine(LineViewModel line)
    {
        if (Workspace is null)
            return;

        Workspace.Lines.Remove(line);

        foreach (var other in Workspace.Lines)
        {
            DetachPointIfOnLine(other, EndpointKind.P1, line);
            DetachPointIfOnLine(other, EndpointKind.P2, line);
        }
    }

    private static void DetachPointIfOnLine(LineViewModel line, EndpointKind endpointKind, LineViewModel removedLine)
    {
        var endpoint = GetEndpoint(line, endpointKind);
        if (endpoint is not PointOnLineWorkspacePoint onLine || !ReferenceEquals(onLine.ParentLine, removedLine))
            return;

        SetEndpoint(line, endpointKind, new FreeWorkspacePoint(onLine.X, onLine.Y));
    }

    private void DeleteSelectedWorkspaceItems()
    {
        if (Workspace is null)
            return;

        var linesToRemove = Workspace.Lines.Where(l => l.IsSelected).ToList();
        var shapesToRemove = Workspace.Shapes.Where(s => s.IsSelected).ToList();
        var imagesToRemove = Workspace.Images.Where(i => i.IsSelected).ToList();

        foreach (var line in linesToRemove)
            RemoveLine(line);

        if (shapesToRemove.Count == 0 && imagesToRemove.Count == 0)
        {
            UpdateScrollbars();
            return;
        }

        var removedSurfaces = new HashSet<IWorkspaceSurface>();
        foreach (var s in shapesToRemove)
            removedSurfaces.Add(s);
        foreach (var img in imagesToRemove)
            removedSurfaces.Add(img);

        DetachPointsOnRemovedSurfaces(removedSurfaces);

        foreach (var s in shapesToRemove)
            Workspace.Shapes.Remove(s);

        foreach (var img in imagesToRemove)
            Workspace.Images.Remove(img);

        UpdateScrollbars();
    }

    private void DetachPointsOnRemovedSurfaces(IReadOnlySet<IWorkspaceSurface> removedSurfaces)
    {
        if (Workspace is null)
            return;

        if (removedSurfaces.Count == 0)
            return;

        var pointMap = new Dictionary<PointOnShapeWorkspacePoint, FreeWorkspacePoint>();

        foreach (var line in Workspace.Lines)
        {
            ReplaceEndpointIfOnRemovedSurface(line, EndpointKind.P1);
            ReplaceEndpointIfOnRemovedSurface(line, EndpointKind.P2);
        }

        void ReplaceEndpointIfOnRemovedSurface(LineViewModel line, EndpointKind endpointKind)
        {
            var endpoint = GetEndpoint(line, endpointKind);
            if (endpoint is not PointOnShapeWorkspacePoint onShape || !removedSurfaces.Contains(onShape.Shape))
                return;

            if (!pointMap.TryGetValue(onShape, out var free))
            {
                free = new FreeWorkspacePoint(onShape.X, onShape.Y);
                pointMap[onShape] = free;
            }

            SetEndpoint(line, endpointKind, free);
        }
    }

    private void StartSelection(Point startWorld, bool additive)
    {
        _isSelecting = true;
        _selectionStartWorld = startWorld;

        if (!additive)
            ClearSelection();

        UpdateSelectionRect(startWorld);
        SelectionRectangle.Visibility = Visibility.Visible;
    }

    private void UpdateSelectionRect(Point currentWorld)
    {
        var rect = CreateRect(_selectionStartWorld, currentWorld);
        Canvas.SetLeft(SelectionRectangle, rect.X);
        Canvas.SetTop(SelectionRectangle, rect.Y);
        SelectionRectangle.Width = rect.Width;
        SelectionRectangle.Height = rect.Height;
    }

    private void FinalizeSelection(Point endWorld, bool additive)
    {
        if (Workspace is null)
            return;

        _isSelecting = false;
        SelectionRectangle.Visibility = Visibility.Collapsed;

        var rect = CreateRect(_selectionStartWorld, endWorld);
        if (rect.Width <= 1 && rect.Height <= 1)
            return;

        SelectItemsInRect(rect, additive);
    }

    private static Rect CreateRect(Point a, Point b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);
        var w = Math.Abs(a.X - b.X);
        var h = Math.Abs(a.Y - b.Y);
        return new Rect(x, y, w, h);
    }

    private void SelectItemsInRect(Rect rect, bool additive)
    {
        if (Workspace is null)
            return;

        if (!additive)
            ClearSelection();

        foreach (var shape in Workspace.Shapes)
        {
            if (rect.IntersectsWith(shape.Bounds))
                shape.IsSelected = true;
        }

        foreach (var image in Workspace.Images)
        {
            if (rect.IntersectsWith(image.Bounds))
                image.IsSelected = true;
        }

        foreach (var line in Workspace.Lines)
        {
            var bounds = GetLineBounds(line);
            if (rect.IntersectsWith(bounds))
                line.IsSelected = true;
        }
    }

    private static Rect GetLineBounds(LineViewModel line)
    {
        var x1 = line.P1.X;
        var y1 = line.P1.Y;
        var x2 = line.P2.X;
        var y2 = line.P2.Y;
        var x = Math.Min(x1, x2);
        var y = Math.Min(y1, y2);
        var w = Math.Abs(x2 - x1);
        var h = Math.Abs(y2 - y1);
        return new Rect(x, y, w, h);
    }

    private void WorkspaceLine_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Workspace is null)
            return;

        if (sender is not DependencyObject dep)
            return;

        BringToFront(dep);

        if (sender is not FrameworkElement element)
            return;

        if (element.DataContext is not LineViewModel line)
            return;

        HandleSingleClickSelection(line, additive: IsCtrlDown());

        BeginSelectionDrag(primaryLineForSnap: line, e.GetPosition(WorkspaceCanvas));
        element.CaptureMouse();
        e.Handled = true;
    }

    private void WorkspaceLine_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingSelection || Workspace is null)
            return;

        if (sender is not FrameworkElement element || !element.IsMouseCaptured)
            return;

        var currentWorld = ViewportToWorld(e.GetPosition(WorkspaceCanvas));
        var delta = currentWorld - _lastSelectionDragWorld;
        _lastSelectionDragWorld = currentWorld;

        MoveSelectedBy(delta);
        UpdateSelectionDragSnap();

        e.Handled = true;
    }

    private void WorkspaceLine_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingSelection || Workspace is null)
            return;

        if (sender is not FrameworkElement element || !element.IsMouseCaptured)
            return;

        element.ReleaseMouseCapture();
        EndSelectionDrag(applySnap: true);
        e.Handled = true;
    }

    private void WorkspaceShape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Workspace is null)
            return;

        var world = ViewportToWorld(e.GetPosition(WorkspaceCanvas));
        _lastWorkspaceMouseWorld = world;

        if (sender is not DependencyObject dep)
            return;

        BringToFront(dep);

        if (sender is not FrameworkElement element)
            return;

        if (element.DataContext is not WorkspaceShapeViewModel shape)
            return;

        if (IsShiftDown())
        {
            StartDraftLine(world);
            WorkspaceCanvas.CaptureMouse();
            e.Handled = true;
            return;
        }

        HandleSingleClickSelection(shape, additive: IsCtrlDown());

        BeginSelectionDrag(primaryLineForSnap: null, e.GetPosition(WorkspaceCanvas));
        element.CaptureMouse();
        e.Handled = true;
    }

    private void WorkspaceShape_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingSelection || Workspace is null)
            return;

        if (sender is not FrameworkElement element || !element.IsMouseCaptured)
            return;

        var currentWorld = ViewportToWorld(e.GetPosition(WorkspaceCanvas));
        var delta = currentWorld - _lastSelectionDragWorld;
        _lastSelectionDragWorld = currentWorld;

        MoveSelectedBy(delta);
        e.Handled = true;
    }

    private void WorkspaceShape_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingSelection || Workspace is null)
            return;

        if (sender is not FrameworkElement element || !element.IsMouseCaptured)
            return;

        element.ReleaseMouseCapture();
        EndSelectionDrag(applySnap: false);
        e.Handled = true;
    }

    private void WorkspaceImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Workspace is null)
            return;

        var world = ViewportToWorld(e.GetPosition(WorkspaceCanvas));
        _lastWorkspaceMouseWorld = world;

        if (sender is not DependencyObject dep)
            return;

        BringToFront(dep);

        if (sender is not FrameworkElement element)
            return;

        if (element.DataContext is not WorkspaceImageViewModel image)
            return;

        if (IsShiftDown())
        {
            StartDraftLine(world);
            WorkspaceCanvas.CaptureMouse();
            e.Handled = true;
            return;
        }

        HandleSingleClickSelection(image, additive: IsCtrlDown());

        BeginSelectionDrag(primaryLineForSnap: null, e.GetPosition(WorkspaceCanvas));
        element.CaptureMouse();
        e.Handled = true;
    }

    private void WorkspaceImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingSelection || Workspace is null)
            return;

        if (sender is not FrameworkElement element || !element.IsMouseCaptured)
            return;

        var currentWorld = ViewportToWorld(e.GetPosition(WorkspaceCanvas));
        var delta = currentWorld - _lastSelectionDragWorld;
        _lastSelectionDragWorld = currentWorld;

        MoveSelectedBy(delta);
        e.Handled = true;
    }

    private void WorkspaceImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingSelection || Workspace is null)
            return;

        if (sender is not FrameworkElement element || !element.IsMouseCaptured)
            return;

        element.ReleaseMouseCapture();
        EndSelectionDrag(applySnap: false);
        e.Handled = true;
    }

    private void WorkspaceShape_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Workspace is null)
            return;

        if (sender is not FrameworkElement element)
            return;

        if (element.DataContext is not WorkspaceShapeViewModel shape)
            return;

        if (!shape.IsSelected)
            return;

        ShowPrettyFormatMenu(e.GetPosition(WorkspaceCanvas));
        e.Handled = true;
    }

    private void WorkspaceImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Workspace is null)
            return;

        if (sender is not FrameworkElement element)
            return;

        if (element.DataContext is not WorkspaceImageViewModel image)
            return;

        if (!image.IsSelected)
            return;

        ShowPrettyFormatMenu(e.GetPosition(WorkspaceCanvas));
        e.Handled = true;
    }

    private void ShowPrettyFormatMenu(Point position)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = WorkspaceCanvas,
            Placement = PlacementMode.MousePoint,
            VerticalOffset = 14,
        };

        var formatItem = new MenuItem { Header = "Formato Bonito" };
        formatItem.Click += (_, _) => ApplyPrettyFormat();

        menu.Items.Add(formatItem);
        menu.IsOpen = true;
    }

    private void ApplyPrettyFormat()
    {
        if (Workspace is null)
            return;

        var formatter = new RpaInventory.App.Workspace.Formatting.PrettyFormatter(Workspace, _settings);
        formatter.FormatSelection();
        UpdateScrollbars();
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_settings)
        {
            Owner = this
        };

        if (settingsWindow.ShowDialog() == true)
        {
            // Configurações já foram atualizadas no ViewModel
        }
    }

    private void BeginSelectionDrag(LineViewModel? primaryLineForSnap, Point startViewport)
    {
        _isDraggingSelection = true;
        _selectionDragPrimaryLine = primaryLineForSnap;
        _selectionDragAllowSnap = ShouldAllowLineSnap() && primaryLineForSnap is not null;
        _lastSelectionDragWorld = ViewportToWorld(startViewport);
        _selectionDragSnapCandidate = null;
        _selectionDragSnapEndpoint = EndpointKind.P1;
        ShowSnapPreview(null);
    }

    private bool ShouldAllowLineSnap()
    {
        if (Workspace is null)
            return false;

        var selectedLines = Workspace.Lines.Count(l => l.IsSelected);
        var selectedShapes = Workspace.Shapes.Count(s => s.IsSelected);
        var selectedImages = Workspace.Images.Count(i => i.IsSelected);

        return selectedLines == 1 && selectedShapes == 0 && selectedImages == 0;
    }

    private void EndSelectionDrag(bool applySnap)
    {
        if (!_isDraggingSelection)
            return;

        if (applySnap)
            ApplySelectionDragSnap();

        _isDraggingSelection = false;
        _selectionDragAllowSnap = false;
        _selectionDragPrimaryLine = null;
        _selectionDragSnapCandidate = null;
        ShowSnapPreview(null);

        UpdateScrollbars();
    }

    private void UpdateSelectionDragSnap()
    {
        if (!_selectionDragAllowSnap || Workspace is null || _selectionDragPrimaryLine is null)
            return;

        var line = _selectionDragPrimaryLine;

        var p1 = new Point(line.P1.X, line.P1.Y);
        var p2 = new Point(line.P2.X, line.P2.Y);

        var cand1 = FindBestSnapCandidate(p1, excludeLine: line, excludePoint: line.P1);
        var cand2 = FindBestSnapCandidate(p2, excludeLine: line, excludePoint: line.P2);

        if (cand1 is null && cand2 is null)
        {
            _selectionDragSnapCandidate = null;
            ShowSnapPreview(null);
            return;
        }

        if (cand1 is null)
        {
            _selectionDragSnapCandidate = cand2;
            _selectionDragSnapEndpoint = EndpointKind.P2;
            ShowSnapPreview(cand2);
            return;
        }

        if (cand2 is null || cand1.Distance <= cand2.Distance)
        {
            _selectionDragSnapCandidate = cand1;
            _selectionDragSnapEndpoint = EndpointKind.P1;
            ShowSnapPreview(cand1);
            return;
        }

        _selectionDragSnapCandidate = cand2;
        _selectionDragSnapEndpoint = EndpointKind.P2;
        ShowSnapPreview(cand2);
    }

    private void ApplySelectionDragSnap()
    {
        if (Workspace is null || _selectionDragPrimaryLine is null || _selectionDragSnapCandidate is null)
            return;

        var line = _selectionDragPrimaryLine;
        var endpointPoint = _selectionDragSnapEndpoint == EndpointKind.P1 ? line.P1 : line.P2;

        if (endpointPoint is not FreeWorkspacePoint)
            return;

        SetEndpoint(line, _selectionDragSnapEndpoint, _selectionDragSnapCandidate.CreatePoint());
    }

    private void HandleSingleClickSelection(LineViewModel line, bool additive)
    {
        if (Workspace is null)
            return;

        if (additive)
        {
            line.IsSelected = !line.IsSelected;
            return;
        }

        if (line.IsSelected)
            return;

        ClearSelection();
        line.IsSelected = true;
    }

    private void HandleSingleClickSelection(WorkspaceShapeViewModel shape, bool additive)
    {
        if (Workspace is null)
            return;

        if (additive)
        {
            shape.IsSelected = !shape.IsSelected;
            return;
        }

        if (shape.IsSelected)
            return;

        ClearSelection();
        shape.IsSelected = true;
    }

    private void HandleSingleClickSelection(WorkspaceImageViewModel image, bool additive)
    {
        if (Workspace is null)
            return;

        if (additive)
        {
            image.IsSelected = !image.IsSelected;
            return;
        }

        if (image.IsSelected)
            return;

        ClearSelection();
        image.IsSelected = true;
    }

    private void ClearSelection()
    {
        if (Workspace is null)
            return;

        foreach (var line in Workspace.Lines)
            line.IsSelected = false;

        foreach (var shape in Workspace.Shapes)
            shape.IsSelected = false;

        foreach (var image in Workspace.Images)
            image.IsSelected = false;
    }

    private void MoveSelectedBy(Vector delta)
    {
        if (Workspace is null)
            return;

        var movedLines = new HashSet<LineViewModel>();
        var movedSurfaces = new HashSet<IWorkspaceSurface>();
        var movedFreePoints = new HashSet<FreeWorkspacePoint>();

        foreach (var shape in Workspace.Shapes.Where(s => s.IsSelected))
            MoveSurfaceBy(shape, delta, movedSurfaces);

        foreach (var image in Workspace.Images.Where(i => i.IsSelected))
            MoveSurfaceBy(image, delta, movedSurfaces);

        foreach (var line in Workspace.Lines.Where(l => l.IsSelected))
            MoveLineBy(line, delta, movedLines, movedSurfaces, movedFreePoints);
    }

    private static void MoveSurfaceBy(IMovableWorkspaceSurface surface, Vector delta, ISet<IWorkspaceSurface> moved)
    {
        if (moved.Contains(surface))
            return;

        moved.Add(surface);
        surface.MoveBy(delta.X, delta.Y);
    }

    private static void MoveLineBy(
        LineViewModel line,
        Vector delta,
        ISet<LineViewModel> movedLines,
        ISet<IWorkspaceSurface> movedSurfaces,
        ISet<FreeWorkspacePoint> movedFreePoints)
    {
        if (movedLines.Contains(line))
            return;

        movedLines.Add(line);

        MovePointOwnerBy(line.P1, delta, movedLines, movedSurfaces, movedFreePoints);
        MovePointOwnerBy(line.P2, delta, movedLines, movedSurfaces, movedFreePoints);
    }

    private static void MovePointOwnerBy(
        IMovableWorkspacePoint point,
        Vector delta,
        ISet<LineViewModel> movedLines,
        ISet<IWorkspaceSurface> movedSurfaces,
        ISet<FreeWorkspacePoint> movedFreePoints)
    {
        if (point is FreeWorkspacePoint free)
        {
            if (movedFreePoints.Contains(free))
                return;

            movedFreePoints.Add(free);
            free.MoveTo(free.X + delta.X, free.Y + delta.Y);
            return;
        }

        if (point is PointOnShapeWorkspacePoint onShape && onShape.Shape is IMovableWorkspaceSurface shapeSurface)
        {
            MoveSurfaceBy(shapeSurface, delta, movedSurfaces);
            return;
        }

        if (point is PointOnLineWorkspacePoint onLine)
        {
            MoveLineBy(onLine.ParentLine, delta, movedLines, movedSurfaces, movedFreePoints);
            return;
        }

        point.MoveTo(point.X + delta.X, point.Y + delta.Y);
    }

    private void BringToFront(DependencyObject source)
    {
        var presenter = FindAncestor<ContentPresenter>(source);
        if (presenter is null)
            return;

        Panel.SetZIndex(presenter, ++_nextZIndex);
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        var node = current;
        while (node is not null)
        {
            if (node is T typed)
                return typed;

            node = VisualTreeHelper.GetParent(node);
        }

        return null;
    }

    private void Endpoint_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Workspace is null)
            return;

        if (sender is not DependencyObject dep)
            return;

        BringToFront(dep);

        if (sender is not FrameworkElement element)
            return;

        if (element.DataContext is not LineViewModel line)
            return;

        var endpointKind = GetEndpointKind(element);

        HandleSingleClickSelection(line, additive: IsCtrlDown());

        if (IsAltDown())
            DetachEndpointIfNeeded(line, endpointKind);

        _isDraggingEndpoint = true;
        _endpointDragLine = line;
        _endpointDragEndpoint = endpointKind;
        _endpointDragSnapCandidate = null;
        element.CaptureMouse();
        e.Handled = true;
    }

    private void Endpoint_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingEndpoint || Workspace is null || _endpointDragLine is null)
            return;

        if (sender is not FrameworkElement element || !element.IsMouseCaptured)
            return;

        var world = ViewportToWorld(e.GetPosition(WorkspaceCanvas));
        var endpoint = GetEndpoint(_endpointDragLine, _endpointDragEndpoint);
        endpoint.MoveTo(world.X, world.Y);

        if (endpoint is FreeWorkspacePoint)
        {
            _endpointDragSnapCandidate = FindBestSnapCandidate(world, excludeLine: _endpointDragLine, excludePoint: endpoint);
            ShowSnapPreview(_endpointDragSnapCandidate);
        }
        else
        {
            _endpointDragSnapCandidate = null;
            ShowSnapPreview(null);
        }

        e.Handled = true;
    }

    private void Endpoint_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingEndpoint || Workspace is null || _endpointDragLine is null)
            return;

        if (sender is not FrameworkElement element || !element.IsMouseCaptured)
            return;

        element.ReleaseMouseCapture();

        var endpoint = GetEndpoint(_endpointDragLine, _endpointDragEndpoint);
        if (endpoint is FreeWorkspacePoint && _endpointDragSnapCandidate is not null)
            SetEndpoint(_endpointDragLine, _endpointDragEndpoint, _endpointDragSnapCandidate.CreatePoint());

        _isDraggingEndpoint = false;
        _endpointDragLine = null;
        _endpointDragSnapCandidate = null;
        ShowSnapPreview(null);
        UpdateScrollbars();
        e.Handled = true;
    }

    private static EndpointKind GetEndpointKind(FrameworkElement endpointElement)
    {
        if (endpointElement is Ellipse { Tag: string tag } && tag.Equals("P2", StringComparison.OrdinalIgnoreCase))
            return EndpointKind.P2;

        return EndpointKind.P1;
    }

    private static IMovableWorkspacePoint GetEndpoint(LineViewModel line, EndpointKind kind)
        => kind == EndpointKind.P1 ? line.P1 : line.P2;

    private static void SetEndpoint(LineViewModel line, EndpointKind kind, IMovableWorkspacePoint point)
    {
        if (kind == EndpointKind.P1)
            line.P1 = point;
        else
            line.P2 = point;
    }

    private static void DetachEndpointIfNeeded(LineViewModel line, EndpointKind kind)
    {
        var endpoint = GetEndpoint(line, kind);
        if (endpoint is FreeWorkspacePoint)
            return;

        SetEndpoint(line, kind, new FreeWorkspacePoint(endpoint.X, endpoint.Y));
    }

    private SnapCandidate? FindBestSnapCandidate(Point worldPoint, LineViewModel? excludeLine, IMovableWorkspacePoint? excludePoint)
    {
        if (Workspace is null)
            return null;

        var thresholdWorld = SnapThresholdPixels / GetWorldScale(WorldTransform.Matrix);

        SnapCandidate? best = null;
        var bestDistance = thresholdWorld;

        foreach (var line in Workspace.Lines)
        {
            if (ReferenceEquals(line, excludeLine))
                continue;

            var p1 = new Point(line.P1.X, line.P1.Y);
            var p2 = new Point(line.P2.X, line.P2.Y);

            ConsiderExistingPoint(line.P1, p1);
            ConsiderExistingPoint(line.P2, p2);

            var projection = WorkspaceGeometry.ProjectPointOntoSegment(worldPoint, p1, p2);
            ConsiderPointOnLine(line, projection);
        }

        foreach (var shape in Workspace.Shapes)
        {
            var candidatePoint = GetClosestPointOnShape(shape, worldPoint);
            ConsiderPointOnShape(shape, candidatePoint);
        }

        foreach (var image in Workspace.Images)
        {
            var candidatePoint = GetClosestPointOnRect(image, worldPoint);
            ConsiderPointOnShape(image, candidatePoint);
        }

        return best;

        void ConsiderExistingPoint(IMovableWorkspacePoint point, Point pointWorld)
        {
            if (ReferenceEquals(point, excludePoint))
                return;

            var distance = (worldPoint - pointWorld).Length;
            if (distance >= bestDistance)
                return;

            bestDistance = distance;
            best = new ExistingPointCandidate(point, pointWorld, distance);
        }

        void ConsiderPointOnLine(LineViewModel line, ProjectionResult projection)
        {
            if (projection.Distance >= bestDistance)
                return;

            bestDistance = projection.Distance;
            best = new PointOnLineCandidate(line, projection.T, projection.Projection, projection.Distance);
        }

        void ConsiderPointOnShape(IWorkspaceSurface shape, Point shapeWorldPoint)
        {
            var distance = (worldPoint - shapeWorldPoint).Length;
            if (distance >= bestDistance)
                return;

            bestDistance = distance;
            best = new PointOnShapeCandidate(
                shape,
                LocalX: shapeWorldPoint.X - shape.X,
                LocalY: shapeWorldPoint.Y - shape.Y,
                WorldPoint: shapeWorldPoint,
                Distance: distance);
        }
    }

    private static Point GetClosestPointOnRect(IWorkspaceSurface surface, Point point)
    {
        var left = surface.X;
        var top = surface.Y;
        var right = surface.X + surface.Width;
        var bottom = surface.Y + surface.Height;
        var centerX = surface.X + (surface.Width / 2);
        var centerY = surface.Y + (surface.Height / 2);

        // Pontos de snap: 4 vértices + 4 meios das arestas
        var snapPoints = new[]
        {
            new Point(left, top),      // Vértice top-left
            new Point(centerX, top),    // Meio topo
            new Point(right, top),      // Vértice top-right
            new Point(right, centerY),  // Meio direita
            new Point(right, bottom),   // Vértice bottom-right
            new Point(centerX, bottom), // Meio baixo
            new Point(left, bottom),   // Vértice bottom-left
            new Point(left, centerY),  // Meio esquerda
        };

        // Encontrar o ponto de snap mais próximo
        var bestPoint = snapPoints[0];
        var minDistance = (point - bestPoint).Length;

        foreach (var snapPoint in snapPoints)
        {
            var distance = (point - snapPoint).Length;
            if (distance < minDistance)
            {
                minDistance = distance;
                bestPoint = snapPoint;
            }
        }

        // Se estiver muito longe, usar o comportamento original
        var threshold = 10.0; // pixels em world
        if (minDistance > threshold)
        {
            var clampedX = Math.Clamp(point.X, left, right);
            var clampedY = Math.Clamp(point.Y, top, bottom);

            var inside = point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom;
            if (!inside)
                return new Point(clampedX, clampedY);

            var dLeft = point.X - left;
            var dRight = right - point.X;
            var dTop = point.Y - top;
            var dBottom = bottom - point.Y;

            var min = Math.Min(Math.Min(dLeft, dRight), Math.Min(dTop, dBottom));
            if (min == dLeft)
                return new Point(left, clampedY);
            if (min == dRight)
                return new Point(right, clampedY);
            if (min == dTop)
                return new Point(clampedX, top);

            return new Point(clampedX, bottom);
        }

        return bestPoint;
    }

    private static Point GetClosestPointOnShape(WorkspaceShapeViewModel shape, Point point)
    {
        if (shape.Kind == WorkspaceShapeKind.Start)
            return GetClosestPointOnStart(shape, point);

        if (shape.Kind is not (WorkspaceShapeKind.Diamond or WorkspaceShapeKind.LogicDecision))
            return GetClosestPointOnRect(shape, point);

        var cx = shape.X + (shape.Width / 2);
        var cy = shape.Y + (shape.Height / 2);

        var top = new Point(cx, shape.Y);
        var right = new Point(shape.X + shape.Width, cy);
        var bottom = new Point(cx, shape.Y + shape.Height);
        var left = new Point(shape.X, cy);

        var best = WorkspaceGeometry.ProjectPointOntoSegment(point, top, right);
        best = Min(best, WorkspaceGeometry.ProjectPointOntoSegment(point, right, bottom));
        best = Min(best, WorkspaceGeometry.ProjectPointOntoSegment(point, bottom, left));
        best = Min(best, WorkspaceGeometry.ProjectPointOntoSegment(point, left, top));

        return best.Projection;

        static ProjectionResult Min(ProjectionResult a, ProjectionResult b)
            => b.Distance < a.Distance ? b : a;
    }

    private static Point GetClosestPointOnStart(WorkspaceShapeViewModel start, Point point)
    {
        var cx = start.X + (start.Width / 2);
        var cy = start.Y + (start.Height / 2);
        var radius = Math.Max(1, Math.Min(start.Width, start.Height) / 2);

        // 4 pontos de snap: topo, direita, baixo, esquerda (formando um +)
        var snapPoints = new[]
        {
            new Point(cx, start.Y),                    // Topo
            new Point(start.X + start.Width, cy),      // Direita
            new Point(cx, start.Y + start.Height),     // Baixo
            new Point(start.X, cy),                     // Esquerda
        };

        // Encontrar o ponto de snap mais próximo
        var bestPoint = snapPoints[0];
        var minDistance = (point - bestPoint).Length;

        foreach (var snapPoint in snapPoints)
        {
            var distance = (point - snapPoint).Length;
            if (distance < minDistance)
            {
                minDistance = distance;
                bestPoint = snapPoint;
            }
        }

        // Se estiver muito longe, usar projeção radial normal
        var threshold = 10.0; // pixels em world
        if (minDistance > threshold)
        {
            var v = new Vector(point.X - cx, point.Y - cy);
            if (v.Length <= double.Epsilon)
                return new Point(cx + radius, cy);

            v.Normalize();
            return new Point(cx + (v.X * radius), cy + (v.Y * radius));
        }

        return bestPoint;
    }

    private void ShowSnapPreview(SnapCandidate? candidate)
    {
        if (Workspace is null)
            return;

        if (candidate is null)
        {
            Workspace.SnapPreview.Hide();
            return;
        }

        Workspace.SnapPreview.Show(candidate.WorldPoint.X, candidate.WorldPoint.Y);
    }

    private enum EndpointKind
    {
        P1,
        P2,
    }

    private abstract record SnapCandidate(Point WorldPoint, double Distance)
    {
        public abstract IMovableWorkspacePoint CreatePoint();
    }

    private sealed record ExistingPointCandidate(IMovableWorkspacePoint Point, Point WorldPoint, double Distance)
        : SnapCandidate(WorldPoint, Distance)
    {
        public override IMovableWorkspacePoint CreatePoint() => Point;
    }

    private sealed record PointOnLineCandidate(LineViewModel Line, double T, Point WorldPoint, double Distance)
        : SnapCandidate(WorldPoint, Distance)
    {
        public override IMovableWorkspacePoint CreatePoint() => new PointOnLineWorkspacePoint(Line, T);
    }

    private sealed record PointOnShapeCandidate(IWorkspaceSurface Shape, double LocalX, double LocalY, Point WorldPoint, double Distance)
        : SnapCandidate(WorldPoint, Distance)
    {
        public override IMovableWorkspacePoint CreatePoint() => new PointOnShapeWorkspacePoint(Shape, LocalX, LocalY);
    }
}
