using System.Windows;
using RpaInventory.App.Inventory.Catalog;
using RpaInventory.App.Inventory.ViewModels;

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
        var mainViewModel = new MainViewModel(catalog);

        var window = new MainWindow
        {
            DataContext = mainViewModel,
        };

        window.Show();
    }
}

