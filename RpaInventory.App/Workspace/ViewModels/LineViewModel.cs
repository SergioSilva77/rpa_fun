using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.Workspace.ViewModels;

public sealed class LineViewModel : ViewModelBase
{
    private bool _isSelected;
    private IMovableWorkspacePoint _p1;
    private IMovableWorkspacePoint _p2;

    public LineViewModel(IMovableWorkspacePoint p1, IMovableWorkspacePoint p2)
    {
        _p1 = p1;
        _p2 = p2;
    }

    public event EventHandler? EndpointsChanged;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public IMovableWorkspacePoint P1
    {
        get => _p1;
        set
        {
            if (SetProperty(ref _p1, value))
                EndpointsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public IMovableWorkspacePoint P2
    {
        get => _p2;
        set
        {
            if (SetProperty(ref _p2, value))
                EndpointsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
