using System.ComponentModel;
using System.Windows;
using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.Workspace.ViewModels;

public sealed class PointOnShapeWorkspacePoint : ViewModelBase, IMovableWorkspacePoint
{
    private readonly IWorkspaceSurface _shape;
    private double _localX;
    private double _localY;

    public PointOnShapeWorkspacePoint(IWorkspaceSurface shape, double localX, double localY)
    {
        _shape = shape;
        _localX = localX;
        _localY = localY;

        _shape.PropertyChanged += Shape_PropertyChanged;
    }

    public IWorkspaceSurface Shape => _shape;

    public double LocalX
    {
        get => _localX;
        private set
        {
            if (SetProperty(ref _localX, value))
                RaisePositionChanged();
        }
    }

    public double LocalY
    {
        get => _localY;
        private set
        {
            if (SetProperty(ref _localY, value))
                RaisePositionChanged();
        }
    }

    public double X => _shape.X + _localX;
    public double Y => _shape.Y + _localY;

    public void MoveTo(double x, double y)
    {
        var newLocalX = x - _shape.X;
        var newLocalY = y - _shape.Y;

        newLocalX = Math.Clamp(newLocalX, 0, _shape.Width);
        newLocalY = Math.Clamp(newLocalY, 0, _shape.Height);

        LocalX = newLocalX;
        LocalY = newLocalY;
    }

    private void Shape_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IWorkspaceSurface.X) or nameof(IWorkspaceSurface.Y) or nameof(IWorkspaceSurface.Width) or nameof(IWorkspaceSurface.Height))
            RaisePositionChanged();
    }

    private void RaisePositionChanged()
    {
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
    }
}

