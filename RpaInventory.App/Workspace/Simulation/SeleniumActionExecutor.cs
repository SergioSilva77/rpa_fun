using RpaInventory.App.Inventory.Catalog;
using RpaInventory.App.Inventory.Items;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Workspace.Simulation;

public static class SeleniumActionExecutor
{
    private static readonly Dictionary<WorkspaceShapeKind, string> ShapeKindToItemId = new()
    {
        { WorkspaceShapeKind.SeleniumOpenBrowser, "browser.open" },
        { WorkspaceShapeKind.SeleniumClick, "browser.click" },
        { WorkspaceShapeKind.SeleniumWait, "browser.wait" },
        { WorkspaceShapeKind.SeleniumType, "browser.type" },
        { WorkspaceShapeKind.SeleniumGetValue, "browser.getvalue" },
    };

    public static bool TryExecuteAction(WorkspaceShapeViewModel shape, IInventoryCatalog catalog, IExecutionContext context)
    {
        if (!ShapeKindToItemId.TryGetValue(shape.Kind, out var itemId))
            return false;

        // Buscar o item no catálogo
        var allItems = GetAllItems(catalog);
        var item = allItems.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return false;

        try
        {
            item.Execute(context);
            return true;
        }
        catch (Exception ex)
        {
            context.ShowError(shape.DisplayName, $"Erro ao executar ação: {ex.Message}");
            return false;
        }
    }

    private static IEnumerable<IInventoryItem> GetAllItems(IInventoryCatalog catalog)
    {
        foreach (var section in catalog.TopSections)
        {
            foreach (var item in catalog.GetItems(section.Id))
                yield return item;
        }

        foreach (var section in catalog.BottomSections)
        {
            foreach (var item in catalog.GetItems(section.Id))
                yield return item;
        }
    }
}

