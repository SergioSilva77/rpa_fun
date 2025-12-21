using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.Workspace.ViewModels;

public sealed class FreeWorkspacePoint : ViewModelBase, IMovableWorkspacePoint
{
    private double _x;
    private double _y;

    public FreeWorkspacePoint(double x, double y)
    {
        _x = x;
        _y = y;
    }

    public double X
    {
        get => _x;
        private set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        private set => SetProperty(ref _y, value);
    }

    public void MoveTo(double x, double y)
    {
        X = x;
        Y = y;
    }
}

