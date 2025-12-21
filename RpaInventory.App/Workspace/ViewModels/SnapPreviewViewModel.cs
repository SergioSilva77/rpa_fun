using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.Workspace.ViewModels;

public sealed class SnapPreviewViewModel : ViewModelBase
{
    private bool _isVisible;
    private double _x;
    private double _y;

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
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

    public void Hide() => IsVisible = false;

    public void Show(double x, double y)
    {
        X = x;
        Y = y;
        IsVisible = true;
    }
}

