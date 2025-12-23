using System.Windows;
using System.Windows.Media;
using OpenQA.Selenium;
using RpaInventory.App.Inventory.Sections;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Inventory.Items.Browser;

public sealed class BrowserGetValueItem : IInventoryItem, IWorkspacePlaceableInventoryItem
{
    public string Id => "browser.getvalue";
    public string DisplayName => "Obter Valor";
    public string? Description => "Obtém o valor de um elemento da página.";
    public InventorySectionId SectionId => InventorySectionId.Bottom1;
    public int? SlotIndex => 4;
    public Geometry? Icon => null;
    public string? IconText => "GET";

    public void Execute(IExecutionContext context)
    {
        if (context.Browser is null)
            return;

        try
        {
            // TODO: Obter seletor de forma interativa/configurável
        }
        catch
        {
            // Silenciosamente falhar
        }
    }

    public void PlaceOnWorkspace(WorkspaceViewModel workspace, Point position)
    {
        const double size = 60;
        workspace.Shapes.Add(new WorkspaceShapeViewModel(
            WorkspaceShapeKind.SeleniumGetValue,
            x: position.X - (size / 2),
            y: position.Y - (size / 2),
            width: size,
            height: size,
            displayName: DisplayName));
    }
}

