using System.Windows;
using System.Windows.Media;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RpaInventory.App.Inventory.Sections;
using RpaInventory.App.Workspace.ViewModels;

namespace RpaInventory.App.Inventory.Items.Browser;

public sealed class BrowserClickItem : IInventoryItem, IWorkspacePlaceableInventoryItem
{
    public string Id => "browser.click";
    public string DisplayName => "Clicar";
    public string? Description => "Clica em um elemento da página (seletor CSS necessário).";
    public InventorySectionId SectionId => InventorySectionId.Bottom1;
    public int? SlotIndex => 1;
    public Geometry? Icon => null;
    public string? IconText => "CLI";

    public void Execute(IExecutionContext context)
    {
        if (context.Browser is null)
        {
            context.ShowError(DisplayName, "Navegador não está aberto. Abra o navegador primeiro.");
            return;
        }

        try
        {
            // TODO: Obter seletor de forma interativa/configurável
            // Placeholder - em produção, obteria o seletor de configuração do shape
            context.ShowInfo(DisplayName, "Funcionalidade de clicar será implementada com configuração de seletor.");
        }
        catch (Exception ex)
        {
            context.ShowError(DisplayName, $"Erro ao clicar: {ex.Message}");
        }
    }

    public void PlaceOnWorkspace(WorkspaceViewModel workspace, Point position)
    {
        const double size = 60;
        workspace.Shapes.Add(new WorkspaceShapeViewModel(
            WorkspaceShapeKind.SeleniumClick,
            x: position.X - (size / 2),
            y: position.Y - (size / 2),
            width: size,
            height: size,
            displayName: DisplayName));
    }
}

