using RpaInventory.App.Inventory.Items;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.Catalog;

public interface IInventoryCatalog
{
    IReadOnlyList<InventorySection> TopSections { get; }
    IReadOnlyList<InventorySection> BottomSections { get; }
    IReadOnlyList<IInventoryItem> GetItems(InventorySectionId sectionId);
}

