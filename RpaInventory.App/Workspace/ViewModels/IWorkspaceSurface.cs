using System.ComponentModel;

namespace RpaInventory.App.Workspace.ViewModels;

public interface IWorkspaceSurface : INotifyPropertyChanged
{
    double X { get; }
    double Y { get; }
    double Width { get; }
    double Height { get; }
}

public interface IMovableWorkspaceSurface : IWorkspaceSurface
{
    void MoveBy(double dx, double dy);
}

