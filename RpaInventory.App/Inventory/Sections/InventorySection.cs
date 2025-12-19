using System.Windows.Media;

namespace RpaInventory.App.Inventory.Sections;

public sealed class InventorySection
{
    public InventorySection(InventorySectionId id, string displayName, Geometry? icon, string? iconText = null)
    {
        Id = id;
        DisplayName = displayName;
        Icon = icon;
        IconText = iconText;
    }

    public InventorySectionId Id { get; }
    public string DisplayName { get; }
    public Geometry? Icon { get; }
    public string? IconText { get; }
}

