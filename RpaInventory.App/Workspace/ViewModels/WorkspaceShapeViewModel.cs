using System.Windows;
using System.Windows.Media;
using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.Workspace.ViewModels;

public sealed class WorkspaceShapeViewModel : ViewModelBase, IMovableWorkspaceSurface
{
    private const double MinSize = 50;
    private bool _isSelected;
    private double _x;
    private double _y;
    private double _width;
    private double _height;

    public WorkspaceShapeViewModel(WorkspaceShapeKind kind, double x, double y, double width, double height, string? displayName = null)
    {
        Kind = kind;
        _x = x;
        _y = y;
        _width = Math.Max(MinSize, width);
        _height = Math.Max(MinSize, height);
        DisplayName = displayName ?? kind.ToString();
    }

    public WorkspaceShapeKind Kind { get; }

    public string DisplayName { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public double X
    {
        get => _x;
        private set
        {
            if (SetProperty(ref _x, value))
                OnPropertyChanged(nameof(Bounds));
        }
    }

    public double Y
    {
        get => _y;
        private set
        {
            if (SetProperty(ref _y, value))
                OnPropertyChanged(nameof(Bounds));
        }
    }

    public double Width
    {
        get => _width;
        private set
        {
            if (SetProperty(ref _width, value))
            {
                OnPropertyChanged(nameof(Bounds));
                OnPropertyChanged(nameof(DiamondPoints));
            }
        }
    }

    public double Height
    {
        get => _height;
        private set
        {
            if (SetProperty(ref _height, value))
            {
                OnPropertyChanged(nameof(Bounds));
                OnPropertyChanged(nameof(DiamondPoints));
            }
        }
    }

    public Rect Bounds => new(X, Y, Width, Height);

    public PointCollection DiamondPoints
        => new()
        {
            new Point(Width / 2, 0),
            new Point(Width, Height / 2),
            new Point(Width / 2, Height),
            new Point(0, Height / 2),
        };

    public void MoveBy(double dx, double dy)
    {
        X += dx;
        Y += dy;
    }

    public void Resize(double width, double height)
    {
        Width = Math.Max(MinSize, width);
        Height = Math.Max(MinSize, height);
    }
}
