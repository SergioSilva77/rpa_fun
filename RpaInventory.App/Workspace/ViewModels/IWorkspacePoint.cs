using System.ComponentModel;

namespace RpaInventory.App.Workspace.ViewModels;

public interface IWorkspacePoint : INotifyPropertyChanged
{
    double X { get; }
    double Y { get; }
}

public interface IMovableWorkspacePoint : IWorkspacePoint
{
    void MoveTo(double x, double y);
}

