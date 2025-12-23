using System.Windows;
using System.Windows.Media;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RpaInventory.App.Inventory.Sections;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Inventory.Items.Browser;

public sealed class BrowserWaitItem : IInventoryItem, IWorkspacePlaceableInventoryItem
{
    public string Id => "browser.wait";
    public string DisplayName => "Esperar";
    public string? Description => "Aguarda um tempo ou elemento aparecer.";
    public InventorySectionId SectionId => InventorySectionId.Bottom1;
    public int? SlotIndex => 2;
    public Geometry? Icon => null;
    public string? IconText => "ESP";

    public void Execute(IExecutionContext context)
    {
        if (context.Browser is null)
            return;

        try
        {
            // Aguarda 2 segundos por padrão
            Thread.Sleep(2000);
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
            WorkspaceShapeKind.SeleniumWait,
            x: position.X - (size / 2),
            y: position.Y - (size / 2),
            width: size,
            height: size,
            displayName: DisplayName));
    }
}

