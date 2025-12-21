using System.Windows;
using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Inventory.Items.Shapes;

public sealed class ShapeSquareItem : IInventoryItem, IWorkspacePlaceableInventoryItem
{
    public string Id => "shape.square";
    public string DisplayName => "Quadrado";
    public string? Description => "Quadrado conectável (arraste para a área de trabalho).";
    public InventorySectionId SectionId => InventorySectionId.Shapes;
    public int? SlotIndex => 2;
    public Geometry? Icon => null;
    public string? IconText => "QUA";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Arraste para a área de trabalho para criar um quadrado.");

    public void PlaceOnWorkspace(WorkspaceViewModel workspace, Point position)
    {
        const double size = 120;
        workspace.Shapes.Add(new WorkspaceShapeViewModel(
            WorkspaceShapeKind.Square,
            x: position.X - (size / 2),
            y: position.Y - (size / 2),
            width: size,
            height: size,
            displayName: DisplayName));
    }
}

