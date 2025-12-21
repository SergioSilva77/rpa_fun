using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.Items.Shapes;

public sealed class ShapeTriangleItem : IInventoryItem
{
    public string Id => "shape.triangle";
    public string DisplayName => "Triângulo";
    public string? Description => "Placeholder (ainda não implementado).";
    public InventorySectionId SectionId => InventorySectionId.Shapes;
    public int? SlotIndex => 6;
    public Geometry? Icon => null;
    public string? IconText => "TRI";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Ainda não implementado.");
}

