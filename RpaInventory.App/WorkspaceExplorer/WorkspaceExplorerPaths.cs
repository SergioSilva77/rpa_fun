using System;
using System.IO;

namespace RpaInventory.App.WorkspaceExplorer;

public static class WorkspaceExplorerPaths
{
    public static string GetDefaultRootPath()
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(basePath, "RpaInventory", "Workspace");
    }
}

