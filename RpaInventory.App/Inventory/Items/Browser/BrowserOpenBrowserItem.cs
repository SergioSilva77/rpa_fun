using System.Windows;
using System.Windows.Media;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using RpaInventory.App.Inventory.Sections;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Inventory.Items.Browser;

public sealed class BrowserOpenBrowserItem : IInventoryItem, IWorkspacePlaceableInventoryItem
{
    public string Id => "browser.open";
    public string DisplayName => "Abrir Navegador";
    public string? Description => "Abre o navegador Chrome.";
    public InventorySectionId SectionId => InventorySectionId.Bottom1;
    public int? SlotIndex => 0;
    public Geometry? Icon => null;
    public string? IconText => "ABR";

    public void Execute(IExecutionContext context)
    {
        // Fechar navegador existente, se houver
        if (context.Browser is not null)
        {
            try
            {
                context.Browser.Quit();
                context.Browser.Dispose();
            }
            catch
            {
                // Ignorar erros ao fechar
            }
            context.Browser = null;
        }

        try
        {
            var driver = new ChromeDriver();
            driver.Manage().Window.Maximize();
            context.Browser = driver;
        }
        catch
        {
            // Silenciosamente falhar
            context.Browser = null;
        }
    }

    public void PlaceOnWorkspace(WorkspaceViewModel workspace, Point position)
    {
        const double size = 60;
        workspace.Shapes.Add(new WorkspaceShapeViewModel(
            WorkspaceShapeKind.SeleniumOpenBrowser,
            x: position.X - (size / 2),
            y: position.Y - (size / 2),
            width: size,
            height: size,
            displayName: DisplayName));
    }
}

