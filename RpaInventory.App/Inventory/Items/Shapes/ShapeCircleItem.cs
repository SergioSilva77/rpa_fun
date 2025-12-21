using System.Windows.Media;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.Items.Shapes;

public sealed class ShapeCircleItem : IInventoryItem
{
    public string Id => "shape.circle";
    public string DisplayName => "Círculo";
    public string? Description => "Placeholder (ainda não implementado).";
    public InventorySectionId SectionId => InventorySectionId.Shapes;
    public int? SlotIndex => 5;
    public Geometry? Icon => null;
    public string? IconText => "CIR";

    public void Execute(IExecutionContext context)
        => context.ShowInfo(DisplayName, "Ainda não implementado.");
}

