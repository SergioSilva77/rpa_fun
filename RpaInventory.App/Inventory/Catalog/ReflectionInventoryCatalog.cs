using System.Reflection;
using RpaInventory.App.Inventory.Items;
using RpaInventory.App.Inventory.Sections;

namespace RpaInventory.App.Inventory.Catalog;

public sealed class ReflectionInventoryCatalog : IInventoryCatalog
{
    private readonly IReadOnlyDictionary<InventorySectionId, IReadOnlyList<IInventoryItem>> _itemsBySection;

    public ReflectionInventoryCatalog()
    {
        TopSections = CreateTopSections();
        BottomSections = CreateBottomSections();
        _itemsBySection = BuildItemsBySection(Assembly.GetExecutingAssembly());
    }

    public IReadOnlyList<InventorySection> TopSections { get; }
    public IReadOnlyList<InventorySection> BottomSections { get; }

    public IReadOnlyList<IInventoryItem> GetItems(InventorySectionId sectionId)
        => _itemsBySection.TryGetValue(sectionId, out var items) ? items : Array.Empty<IInventoryItem>();

    private static IReadOnlyDictionary<InventorySectionId, IReadOnlyList<IInventoryItem>> BuildItemsBySection(Assembly assembly)
    {
        var items = new List<IInventoryItem>();

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            if (!typeof(IInventoryItem).IsAssignableFrom(type))
                continue;

            if (type.GetConstructor(Type.EmptyTypes) is null)
                continue;

            if (Activator.CreateInstance(type) is not IInventoryItem instance)
                continue;

            items.Add(instance);
        }

        return items
            .GroupBy(i => i.SectionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<IInventoryItem>)group
                    .OrderBy(i => i.SlotIndex ?? int.MaxValue)
                    .ThenBy(i => i.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList());
    }

    private static IReadOnlyList<InventorySection> CreateTopSections()
        => new List<InventorySection>
        {
            new(InventorySectionId.Database, "Banco de Dados", icon: null, iconText: "DB"),
            new(InventorySectionId.Spreadsheet, "Planilhas", icon: null, iconText: "XLS"),
            new(InventorySectionId.Browser, "Navegador", icon: null, iconText: "WEB"),
            new(InventorySectionId.Programs, "Programas", icon: null, iconText: "APP"),
            new(InventorySectionId.Scripts, "Scripts", icon: null, iconText: "SCR"),
            new(InventorySectionId.Shapes, "Formas", icon: null, iconText: "SHP"),
            new(InventorySectionId.Rpa, "RPA", icon: null, iconText: "RPA"),
        };

    private static IReadOnlyList<InventorySection> CreateBottomSections()
        => new List<InventorySection>
        {
            new(InventorySectionId.Bottom1, "Seção 1", icon: null, iconText: "1"),
            new(InventorySectionId.Bottom2, "Seção 2", icon: null, iconText: "2"),
            new(InventorySectionId.Bottom3, "Seção 3", icon: null, iconText: "3"),
            new(InventorySectionId.Bottom4, "Seção 4", icon: null, iconText: "4"),
        };
}

