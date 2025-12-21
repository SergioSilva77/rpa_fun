using System.Collections.ObjectModel;
using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.Workspace.ViewModels;

public sealed class WorkspaceViewModel : ViewModelBase
{
    public WorkspaceViewModel()
    {
        Shapes = new ObservableCollection<WorkspaceShapeViewModel>();
        Images = new ObservableCollection<WorkspaceImageViewModel>();
        Lines = new ObservableCollection<LineViewModel>();
        Balls = new ObservableCollection<WorkspaceBallViewModel>();
        DraftLine = new DraftLineViewModel();
        SnapPreview = new SnapPreviewViewModel();
    }

    public ObservableCollection<WorkspaceShapeViewModel> Shapes { get; }
    public ObservableCollection<WorkspaceImageViewModel> Images { get; }
    public ObservableCollection<LineViewModel> Lines { get; }
    public ObservableCollection<WorkspaceBallViewModel> Balls { get; }
    public DraftLineViewModel DraftLine { get; }
    public SnapPreviewViewModel SnapPreview { get; }
}
