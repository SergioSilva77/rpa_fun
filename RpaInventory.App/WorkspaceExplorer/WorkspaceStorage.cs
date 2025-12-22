using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RpaInventory.App.WorkspaceExplorer;

public sealed record WorkspaceFsEntry(string Name, string FullPath, bool IsDirectory);

public sealed class WorkspaceStorage
{
    public const string OrderFileName = ".rpa_inventory.order.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public WorkspaceStorage(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }

    public void EnsureRoot()
        => Directory.CreateDirectory(RootPath);

    public IReadOnlyList<WorkspaceFsEntry> GetChildren(string directoryPath)
    {
        var entries = EnumerateEntries(directoryPath);

        var orderPath = Path.Combine(directoryPath, OrderFileName);
        if (!File.Exists(orderPath))
        {
            return entries
                .OrderByDescending(entry => entry.IsDirectory)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var order = ReadOrder(orderPath);
        return ApplyOrder(entries, order);
    }

    public bool DirectoryHasUserContent(string directoryPath)
    {
        try
        {
            return Directory.EnumerateFileSystemEntries(directoryPath)
                .Any(path => !IsOrderFile(path));
        }
        catch
        {
            return false;
        }
    }

    public void InsertIntoOrder(string directoryPath, string name, int index)
    {
        var order = GetNormalizedOrder(directoryPath);
        order.RemoveAll(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase));

        index = Math.Clamp(index, 0, order.Count);
        order.Insert(index, name);
        WriteOrder(directoryPath, order);
    }

    public void MoveWithinOrder(string directoryPath, string name, int newIndex)
    {
        var order = GetNormalizedOrder(directoryPath);
        var oldIndex = order.FindIndex(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase));
        if (oldIndex < 0)
            return;

        order.RemoveAt(oldIndex);
        newIndex = Math.Clamp(newIndex, 0, order.Count);
        order.Insert(newIndex, name);
        WriteOrder(directoryPath, order);
    }

    public void RemoveFromOrder(string directoryPath, string name)
    {
        var order = GetNormalizedOrder(directoryPath);
        order.RemoveAll(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase));
        WriteOrder(directoryPath, order);
    }

    public void RenameInOrder(string directoryPath, string oldName, string newName)
    {
        var order = ReadOrder(Path.Combine(directoryPath, OrderFileName));
        order.RemoveAll(existing => string.Equals(existing, newName, StringComparison.OrdinalIgnoreCase));

        var index = order.FindIndex(existing => string.Equals(existing, oldName, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            order[index] = newName;
        }
        else
        {
            order.Add(newName);
        }

        var normalized = NormalizeOrder(order, EnumerateEntries(directoryPath));
        WriteOrder(directoryPath, normalized);
    }

    private static bool IsOrderFile(string path)
        => string.Equals(Path.GetFileName(path), OrderFileName, StringComparison.OrdinalIgnoreCase);

    private static List<string> ReadOrder(string orderFilePath)
    {
        try
        {
            if (!File.Exists(orderFilePath))
                return [];

            var json = File.ReadAllText(orderFilePath);
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private List<string> GetNormalizedOrder(string directoryPath)
    {
        var entries = EnumerateEntries(directoryPath);
        var order = ReadOrder(Path.Combine(directoryPath, OrderFileName));
        return NormalizeOrder(order, entries);
    }

    private static List<string> NormalizeOrder(List<string> order, IReadOnlyList<WorkspaceFsEntry> entries)
    {
        var remaining = new HashSet<string>(entries.Select(entry => entry.Name), StringComparer.OrdinalIgnoreCase);
        var normalized = new List<string>(capacity: entries.Count);

        foreach (var name in order)
        {
            if (!remaining.Remove(name))
                continue;

            normalized.Add(name);
        }

        var missing = entries
            .Where(entry => remaining.Contains(entry.Name))
            .OrderByDescending(entry => entry.IsDirectory)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entry => entry.Name);

        normalized.AddRange(missing);
        return normalized;
    }

    private static IReadOnlyList<WorkspaceFsEntry> ApplyOrder(IReadOnlyList<WorkspaceFsEntry> entries, List<string> order)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < order.Count; i++)
        {
            if (!index.ContainsKey(order[i]))
                index.Add(order[i], i);
        }

        return entries
            .OrderBy(entry => index.TryGetValue(entry.Name, out var idx) ? idx : int.MaxValue)
            .ThenBy(entry => index.ContainsKey(entry.Name) ? 0 : entry.IsDirectory ? 0 : 1)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void WriteOrder(string directoryPath, List<string> order)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);

            var orderPath = Path.Combine(directoryPath, OrderFileName);
            var json = JsonSerializer.Serialize(order, JsonOptions);
            File.WriteAllText(orderPath, json);

            try
            {
                var attributes = File.GetAttributes(orderPath);
                if ((attributes & FileAttributes.Hidden) == 0)
                    File.SetAttributes(orderPath, attributes | FileAttributes.Hidden);
            }
            catch
            {
                // Best-effort: esconder o arquivo de ordem no Windows.
            }
        }
        catch
        {
            // Best-effort: falha em persistir ordem não deve quebrar o app.
        }
    }

    private static List<WorkspaceFsEntry> EnumerateEntries(string directoryPath)
    {
        var entries = new List<WorkspaceFsEntry>();

        foreach (var dir in Directory.EnumerateDirectories(directoryPath))
        {
            entries.Add(new WorkspaceFsEntry(Path.GetFileName(dir), dir, IsDirectory: true));
        }

        foreach (var file in Directory.EnumerateFiles(directoryPath))
        {
            if (IsOrderFile(file))
                continue;

            entries.Add(new WorkspaceFsEntry(Path.GetFileName(file), file, IsDirectory: false));
        }

        return entries;
    }
}

