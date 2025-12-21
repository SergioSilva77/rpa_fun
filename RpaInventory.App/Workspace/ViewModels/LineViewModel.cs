using System.ComponentModel;
using System.Windows.Media;
using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.Workspace.ViewModels;

public sealed class LineViewModel : ViewModelBase
{
    private static readonly Brush TrueMarkerBrush = CreateBrush(0x1D, 0xBA, 0x3A);
    private static readonly Brush FalseMarkerBrush = CreateBrush(0xD0, 0x2B, 0x2B);

    private bool _isSelected;
    private LogicBranchKind _branchKind;
    private IMovableWorkspacePoint _p1;
    private IMovableWorkspacePoint _p2;

    public LineViewModel(IMovableWorkspacePoint p1, IMovableWorkspacePoint p2)
    {
        _p1 = p1;
        _p2 = p2;

        SubscribeToEndpoint(_p1);
        SubscribeToEndpoint(_p2);
    }

    public event EventHandler? EndpointsChanged;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public LogicBranchKind BranchKind
    {
        get => _branchKind;
        set
        {
            if (!SetProperty(ref _branchKind, value))
                return;

            OnPropertyChanged(nameof(HasLogicMarker));
            OnPropertyChanged(nameof(MarkerFill));
            OnPropertyChanged(nameof(MarkerText));
        }
    }

    public bool HasLogicMarker => BranchKind != LogicBranchKind.None;

    public double MarkerX => (P1.X + P2.X) / 2;

    public double MarkerY => (P1.Y + P2.Y) / 2;

    public Brush? MarkerFill => BranchKind switch
    {
        LogicBranchKind.True => TrueMarkerBrush,
        LogicBranchKind.False => FalseMarkerBrush,
        _ => null,
    };

    public string? MarkerText => BranchKind switch
    {
        LogicBranchKind.True => "✓",
        LogicBranchKind.False => "✕",
        _ => null,
    };

    public IMovableWorkspacePoint P1
    {
        get => _p1;
        set
        {
            var old = _p1;
            if (!SetProperty(ref _p1, value))
                return;

            UnsubscribeFromEndpoint(old);
            SubscribeToEndpoint(value);
            EndpointsChanged?.Invoke(this, EventArgs.Empty);
            RaiseMarkerChanged();
        }
    }

    public IMovableWorkspacePoint P2
    {
        get => _p2;
        set
        {
            var old = _p2;
            if (!SetProperty(ref _p2, value))
                return;

            UnsubscribeFromEndpoint(old);
            SubscribeToEndpoint(value);
            EndpointsChanged?.Invoke(this, EventArgs.Empty);
            RaiseMarkerChanged();
        }
    }

    private static Brush CreateBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private void SubscribeToEndpoint(IMovableWorkspacePoint point)
        => point.PropertyChanged += Endpoint_PropertyChanged;

    private void UnsubscribeFromEndpoint(IMovableWorkspacePoint point)
        => point.PropertyChanged -= Endpoint_PropertyChanged;

    private void Endpoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IWorkspacePoint.X) or nameof(IWorkspacePoint.Y))
            RaiseMarkerChanged();
    }

    private void RaiseMarkerChanged()
    {
        OnPropertyChanged(nameof(MarkerX));
        OnPropertyChanged(nameof(MarkerY));
    }
}
