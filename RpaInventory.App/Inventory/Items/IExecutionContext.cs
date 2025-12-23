using OpenQA.Selenium;

namespace RpaInventory.App.Inventory.Items;

public interface IExecutionContext
{
    void ShowInfo(string title, string message);
    void ShowError(string title, string message);
    
    IWebDriver? Browser { get; set; }
}

