using System.Windows;
using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Inventory.Items.Shapes;

public sealed class ShapeRectangleItem : IInventoryItem, IWorkspacePlaceableInventoryItem
{
    public string Id => "shape.rectangle";
    public string DisplayName => "Retângulo";
    public string? Description => "Retângulo conectável (arraste para a área de trabalho).";
    public InventorySectionId SectionId => InventorySectionId.Shapes;
    public int? SlotIndex => 1;
    public Geometry? Icon => null;
    public string? IconText => "RET";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Arraste para a área de trabalho para criar um retângulo.");

    public void PlaceOnWorkspace(WorkspaceViewModel workspace, Point position)
    {
        const double size = 60;
        workspace.Shapes.Add(new WorkspaceShapeViewModel(
            WorkspaceShapeKind.Rectangle,
            x: position.X - (size / 2),
            y: position.Y - (size / 2),
            width: size,
            height: size,
            displayName: DisplayName));
    }
}

