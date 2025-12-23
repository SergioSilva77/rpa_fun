using OpenQA.Selenium;

namespace RpaInventory.App.Inventory.Items.Browser;

public interface ISeleniumAction
{
    string ActionId { get; }
    string DisplayName { get; }
    
    void Execute(IWebDriver driver, IExecutionContext context);
}

