using System.Windows;
using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Inventory.Items.Shapes;

public sealed class ShapeLineItem : IInventoryItem, IWorkspacePlaceableInventoryItem
{
    public string Id => "shape.line";
    public string DisplayName => "Linha";
    public string? Description => "Linha conectável (P1/P2). SHIFT+arrastar cria uma nova linha.";
    public InventorySectionId SectionId => InventorySectionId.Shapes;
    public int? SlotIndex => 0;
    public Geometry? Icon => null;
    public string? IconText => "LIN";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Arraste para a área de trabalho ou use SHIFT+arrastar para criar.");

    public void PlaceOnWorkspace(WorkspaceViewModel workspace, Point position)
    {
        var p1 = new FreeWorkspacePoint(position.X, position.Y);
        var p2 = new FreeWorkspacePoint(position.X + 120, position.Y);
        workspace.Lines.Add(new LineViewModel(p1, p2));
    }
}

