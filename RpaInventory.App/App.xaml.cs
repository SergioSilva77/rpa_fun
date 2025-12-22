using System.Windows;
using RpaInventory.App.Inventory.Catalog;
using RpaInventory.App.Inventory.ViewModels;
using RpaInventory.App.WorkspaceExplorer;

namespace RpaInventory.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var catalog = new ReflectionInventoryCatalog();
        var dialogs = new WorkspaceDialogService();
        var mainViewModel = new MainViewModel(catalog, dialogs);

        var window = new MainWindow
        {
            DataContext = mainViewModel,
        };

        window.Show();
    }
}
