using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.Workspace.ViewModels;

public sealed class WorkspaceBallViewModel : ViewModelBase
{
    private double _x;
    private double _y;

    public WorkspaceBallViewModel(double x, double y)
    {
        _x = x;
        _y = y;
    }

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }
}

