using System.Windows.Media;
using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.Workspace.ViewModels;

public sealed class DraftLineViewModel : ViewModelBase
{
    private static readonly Brush TrueMarkerBrush = CreateBrush(0x1D, 0xBA, 0x3A);
    private static readonly Brush FalseMarkerBrush = CreateBrush(0xD0, 0x2B, 0x2B);

    private bool _isActive;
    private LogicBranchKind _branchKind;
    private double _x1;
    private double _y1;
    private double _x2;
    private double _y2;

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
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

    public double MarkerX => (X1 + X2) / 2;

    public double MarkerY => (Y1 + Y2) / 2;

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

    public double X1
    {
        get => _x1;
        set
        {
            if (SetProperty(ref _x1, value))
                RaiseMarkerPositionChanged();
        }
    }

    public double Y1
    {
        get => _y1;
        set
        {
            if (SetProperty(ref _y1, value))
                RaiseMarkerPositionChanged();
        }
    }

    public double X2
    {
        get => _x2;
        set
        {
            if (SetProperty(ref _x2, value))
                RaiseMarkerPositionChanged();
        }
    }

    public double Y2
    {
        get => _y2;
        set
        {
            if (SetProperty(ref _y2, value))
                RaiseMarkerPositionChanged();
        }
    }

    private static Brush CreateBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private void RaiseMarkerPositionChanged()
    {
        OnPropertyChanged(nameof(MarkerX));
        OnPropertyChanged(nameof(MarkerY));
    }
}
