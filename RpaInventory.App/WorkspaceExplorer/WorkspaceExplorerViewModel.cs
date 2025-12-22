using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using RpaInventory.App.Inventory.ViewModels;

namespace RpaInventory.App.WorkspaceExplorer;

public sealed class WorkspaceExplorerViewModel : ViewModelBase
{
    private readonly WorkspaceStorage _storage;
    private WorkspaceTreeNodeViewModel? _selectedNode;
    private string _searchText = string.Empty;
    private WorkspaceSearchFilterOption _selectedSearchFilter;
    private bool _isSearchMode;

    public WorkspaceExplorerViewModel(IWorkspaceDialogService dialogs)
    {
        Dialogs = dialogs;
        RootPath = WorkspaceExplorerPaths.GetDefaultRootPath();
        _storage = new WorkspaceStorage(RootPath);
        _storage.EnsureRoot();

        SearchFilters = new ObservableCollection<WorkspaceSearchFilterOption>
        {
            new(WorkspaceSearchFilter.All, "Tudo"),
            new(WorkspaceSearchFilter.Files, "Arquivo"),
            new(WorkspaceSearchFilter.Folders, "Pasta"),
            new(WorkspaceSearchFilter.Words, "Palavras"),
        };
        _selectedSearchFilter = SearchFilters[0];

        Projects = new ObservableCollection<WorkspaceTreeNodeViewModel>();
        Projects.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasProjects));
        SearchResults = new ObservableCollection<WorkspaceSearchResultViewModel>();
        SearchResults.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSearchResults));

        CreateProjectCommand = new RelayCommand(CreateProject);
        SearchCommand = new RelayCommand(Search);
        ClearSearchCommand = new RelayCommand(ClearSearch);

        Reload();
    }

    public IWorkspaceDialogService Dialogs { get; }
    public string RootPath { get; }

    public ObservableCollection<WorkspaceTreeNodeViewModel> Projects { get; }
    public ObservableCollection<WorkspaceSearchResultViewModel> SearchResults { get; }
    public ObservableCollection<WorkspaceSearchFilterOption> SearchFilters { get; }

    public WorkspaceTreeNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set => SetProperty(ref _selectedNode, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
                return;

            OnPropertyChanged(nameof(HasSearch));

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                SearchResults.Clear();
                IsSearchMode = false;
            }
        }
    }

    public WorkspaceSearchFilterOption SelectedSearchFilter
    {
        get => _selectedSearchFilter;
        set => SetProperty(ref _selectedSearchFilter, value);
    }

    public bool HasProjects => Projects.Count > 0;
    public bool HasSearch => !string.IsNullOrWhiteSpace(SearchText);
    public bool HasSearchResults => SearchResults.Count > 0;

    public bool IsSearchMode
    {
        get => _isSearchMode;
        private set => SetProperty(ref _isSearchMode, value);
    }

    public ICommand CreateProjectCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public void Reload(string? preferredSelectedPath = null)
    {
        var expandedState = CaptureExpandedState();
        var selectedPath = preferredSelectedPath ?? SelectedNode?.FullPath;

        _storage.EnsureRoot();

        Projects.Clear();
        SearchResults.Clear();

        foreach (var entry in _storage.GetChildren(RootPath))
        {
            if (!entry.IsDirectory)
                continue;

            var projectNode = BuildTreeNode(
                parent: null,
                kind: WorkspaceNodeKind.Project,
                entry.Name,
                entry.FullPath,
                expandedState);
            Projects.Add(projectNode);
        }

        if (!string.IsNullOrWhiteSpace(selectedPath))
            TrySelectByPath(selectedPath);
    }

    public void CreateProject()
    {
        var name = Dialogs.PromptText("Criar projeto", "Nome do projeto:", "MeuProjeto");
        if (name is null)
            return;

        if (!TryValidateName(name, out var validatedName, out var error))
        {
            Dialogs.ShowError("Criar projeto", error);
            return;
        }

        var projectPath = Path.Combine(RootPath, validatedName);
        if (Directory.Exists(projectPath) || File.Exists(projectPath))
        {
            Dialogs.ShowError("Criar projeto", "Já existe um projeto com esse nome.");
            return;
        }

        try
        {
            Directory.CreateDirectory(projectPath);
            _storage.InsertIntoOrder(RootPath, validatedName, index: int.MaxValue);
            Reload(preferredSelectedPath: projectPath);
        }
        catch (Exception ex)
        {
            Dialogs.ShowError("Criar projeto", ex.Message);
        }
    }

    public void CreateFile(WorkspaceTreeNodeViewModel parent)
    {
        if (parent.Kind is not (WorkspaceNodeKind.Project or WorkspaceNodeKind.Folder))
            return;

        var name = Dialogs.PromptText("Criar arquivo", "Nome do arquivo:", "workflow.rpa");
        if (name is null)
            return;

        if (!TryValidateName(name, out var validatedName, out var error))
        {
            Dialogs.ShowError("Criar arquivo", error);
            return;
        }

        var filePath = Path.Combine(parent.FullPath, validatedName);
        if (Directory.Exists(filePath))
        {
            Dialogs.ShowError("Criar arquivo", "Já existe uma pasta com esse nome.");
            return;
        }

        var overwrite = false;
        if (File.Exists(filePath))
        {
            overwrite = Dialogs.Confirm("Substituir arquivo?", $"O arquivo '{validatedName}' já existe. Deseja substituir?");
            if (!overwrite)
                return;
        }

        try
        {
            File.WriteAllText(filePath, string.Empty);
            _storage.InsertIntoOrder(parent.FullPath, validatedName, index: int.MaxValue);
            Reload(preferredSelectedPath: filePath);
        }
        catch (Exception ex)
        {
            Dialogs.ShowError("Criar arquivo", ex.Message);
        }
    }

    public void CreateFolder(WorkspaceTreeNodeViewModel parent)
    {
        if (parent.Kind is not (WorkspaceNodeKind.Project or WorkspaceNodeKind.Folder))
            return;

        var name = Dialogs.PromptText("Criar pasta", "Nome da pasta:", "NovaPasta");
        if (name is null)
            return;

        if (!TryValidateName(name, out var validatedName, out var error))
        {
            Dialogs.ShowError("Criar pasta", error);
            return;
        }

        var folderPath = Path.Combine(parent.FullPath, validatedName);
        if (File.Exists(folderPath))
        {
            Dialogs.ShowError("Criar pasta", "Já existe um arquivo com esse nome.");
            return;
        }

        if (Directory.Exists(folderPath))
        {
            var overwrite = Dialogs.Confirm(
                "Substituir pasta?",
                $"A pasta '{validatedName}' já existe. Deseja substituir? (isso pode apagar conteúdo)");
            if (!overwrite)
                return;

            if (_storage.DirectoryHasUserContent(folderPath))
            {
                var confirm = Dialogs.Confirm("Atenção", "A pasta contém itens. Deseja apagar mesmo?");
                if (!confirm)
                    return;
            }

            Directory.Delete(folderPath, recursive: true);
        }

        try
        {
            Directory.CreateDirectory(folderPath);
            _storage.InsertIntoOrder(parent.FullPath, validatedName, index: int.MaxValue);
            Reload(preferredSelectedPath: folderPath);
        }
        catch (Exception ex)
        {
            Dialogs.ShowError("Criar pasta", ex.Message);
        }
    }

    public void Rename(WorkspaceTreeNodeViewModel node)
    {
        var newName = Dialogs.PromptText("Renomear", "Novo nome:", node.Name);
        if (newName is null)
            return;

        if (!TryValidateName(newName, out var validatedName, out var error))
        {
            Dialogs.ShowError("Renomear", error);
            return;
        }

        if (string.Equals(validatedName, node.Name, StringComparison.OrdinalIgnoreCase))
            return;

        var parentDir = node.Parent?.FullPath ?? RootPath;
        var destinationPath = Path.Combine(parentDir, validatedName);

        if (Directory.Exists(destinationPath) || File.Exists(destinationPath))
        {
            var overwrite = Dialogs.Confirm("Substituir?", $"Já existe '{validatedName}'. Deseja substituir?");
            if (!overwrite)
                return;

            if (Directory.Exists(destinationPath) && _storage.DirectoryHasUserContent(destinationPath))
            {
                var confirm = Dialogs.Confirm("Atenção", "O destino contém itens. Deseja apagar mesmo?");
                if (!confirm)
                    return;
            }

            DeletePath(destinationPath);
        }

        try
        {
            if (Directory.Exists(node.FullPath))
            {
                Directory.Move(node.FullPath, destinationPath);
            }
            else
            {
                File.Move(node.FullPath, destinationPath, overwrite: false);
            }

            _storage.RenameInOrder(parentDir, node.Name, validatedName);
            Reload(preferredSelectedPath: destinationPath);
        }
        catch (Exception ex)
        {
            Dialogs.ShowError("Renomear", ex.Message);
        }
    }

    public void Delete(WorkspaceTreeNodeViewModel node)
    {
        var title = node.Kind == WorkspaceNodeKind.Project ? "Deletar projeto" : "Deletar";
        var confirm = Dialogs.Confirm(title, $"Deseja apagar '{node.Name}'?");
        if (!confirm)
            return;

        if (Directory.Exists(node.FullPath) && _storage.DirectoryHasUserContent(node.FullPath))
        {
            var dangerous = Dialogs.Confirm("Atenção", "Este item contém coisas dentro. Deseja apagar mesmo?");
            if (!dangerous)
                return;
        }

        try
        {
            DeletePath(node.FullPath);

            var parentDir = node.Parent?.FullPath ?? RootPath;
            _storage.RemoveFromOrder(parentDir, node.Name);
            Reload(preferredSelectedPath: parentDir);
        }
        catch (Exception ex)
        {
            Dialogs.ShowError(title, ex.Message);
        }
    }

    public void Search()
    {
        try
        {
            SearchResults.Clear();

            var query = SearchText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(query))
            {
                IsSearchMode = false;
                return;
            }

            if (SelectedSearchFilter is null)
            {
                SelectedSearchFilter = SearchFilters.FirstOrDefault() ?? SearchFilters[0];
            }

            _storage.EnsureRoot();

            const int maxResults = 300;
            var queryComparison = StringComparison.OrdinalIgnoreCase;
            var filter = SelectedSearchFilter.Value;

            var results = new List<WorkspaceSearchResultViewModel>(capacity: 64);

            foreach (var projectDir in SafeEnumerateDirectories(RootPath, recursive: false))
            {
                if (results.Count >= maxResults)
                    break;

                SearchDirectory(projectDir, query, queryComparison, filter, results, maxResults);
            }

            foreach (var result in results)
                SearchResults.Add(result);

            IsSearchMode = true;
        }
        catch (Exception ex)
        {
            Dialogs.ShowError("Erro na busca", $"Ocorreu um erro ao buscar: {ex.Message}");
            IsSearchMode = false;
        }
    }

    public void ClearSearch()
        => SearchText = string.Empty;

    public bool TrySelectNodeByPath(string fullPath)
    {
        TrySelectByPath(fullPath);
        return SelectedNode is not null && string.Equals(SelectedNode.FullPath, fullPath, StringComparison.OrdinalIgnoreCase);
    }

    public void MoveNode(WorkspaceTreeNodeViewModel node, WorkspaceTreeNodeViewModel? destinationParent, int destinationIndex)
    {
        if (node.Kind == WorkspaceNodeKind.Project)
        {
            if (destinationParent is not null)
                return;

            _storage.MoveWithinOrder(RootPath, node.Name, destinationIndex);
            Reload(preferredSelectedPath: node.FullPath);
            return;
        }

        if (destinationParent is null || destinationParent.Kind == WorkspaceNodeKind.File)
            return;

        if (ReferenceEquals(node, destinationParent))
            return;

        if (IsAncestor(node, destinationParent))
        {
            Dialogs.ShowError("Mover", "Não é possível mover uma pasta para dentro dela mesma.");
            return;
        }

        var sourceParent = node.Parent?.FullPath;
        if (string.IsNullOrWhiteSpace(sourceParent))
            return;

        var destinationDir = destinationParent.FullPath;
        var sameParent = string.Equals(sourceParent, destinationDir, StringComparison.OrdinalIgnoreCase);

        if (sameParent)
        {
            _storage.MoveWithinOrder(sourceParent, node.Name, destinationIndex);
            Reload(preferredSelectedPath: node.FullPath);
            return;
        }

        var destinationPath = Path.Combine(destinationDir, node.Name);
        if (Directory.Exists(destinationPath) || File.Exists(destinationPath))
        {
            var overwrite = Dialogs.Confirm("Substituir?", $"Já existe '{node.Name}' no destino. Deseja substituir?");
            if (!overwrite)
                return;

            if (Directory.Exists(destinationPath) && _storage.DirectoryHasUserContent(destinationPath))
            {
                var confirm = Dialogs.Confirm("Atenção", "O destino contém itens. Deseja apagar mesmo?");
                if (!confirm)
                    return;
            }

            DeletePath(destinationPath);
        }

        try
        {
            if (Directory.Exists(node.FullPath))
                Directory.Move(node.FullPath, destinationPath);
            else
                File.Move(node.FullPath, destinationPath, overwrite: false);

            _storage.RemoveFromOrder(sourceParent, node.Name);
            _storage.InsertIntoOrder(destinationDir, node.Name, destinationIndex);
            Reload(preferredSelectedPath: destinationPath);
        }
        catch (Exception ex)
        {
            Dialogs.ShowError("Mover", ex.Message);
        }
    }

    private Dictionary<string, bool> CaptureExpandedState()
    {
        var state = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in Projects)
            CaptureExpandedState(project, state);
        return state;
    }

    private static void CaptureExpandedState(WorkspaceTreeNodeViewModel node, Dictionary<string, bool> state)
    {
        state[node.FullPath] = node.IsExpanded;
        foreach (var child in node.Children)
            CaptureExpandedState(child, state);
    }

    private WorkspaceTreeNodeViewModel BuildTreeNode(
        WorkspaceTreeNodeViewModel? parent,
        WorkspaceNodeKind kind,
        string name,
        string fullPath,
        IReadOnlyDictionary<string, bool> expandedState)
    {
        var node = new WorkspaceTreeNodeViewModel(this, parent, kind, name, fullPath)
        {
            IsExpanded = expandedState.TryGetValue(fullPath, out var expanded) && expanded,
        };

        if (kind is WorkspaceNodeKind.Project or WorkspaceNodeKind.Folder)
        {
            foreach (var child in _storage.GetChildren(fullPath))
            {
                var childKind = child.IsDirectory ? WorkspaceNodeKind.Folder : WorkspaceNodeKind.File;
                node.Children.Add(BuildTreeNode(node, childKind, child.Name, child.FullPath, expandedState));
            }
        }

        return node;
    }

    private void TrySelectByPath(string fullPath)
    {
        foreach (var project in Projects)
        {
            if (TrySelectByPath(project, fullPath))
                return;
        }
    }

    private static bool TrySelectByPath(WorkspaceTreeNodeViewModel node, string fullPath)
    {
        if (string.Equals(node.FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
        {
            node.IsSelected = true;
            return true;
        }

        foreach (var child in node.Children)
        {
            if (TrySelectByPath(child, fullPath))
            {
                node.IsExpanded = true;
                return true;
            }
        }

        return false;
    }

    private static bool TryValidateName(string input, out string validated, out string error)
    {
        validated = input.Trim();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(validated))
        {
            error = "Nome não pode ser vazio.";
            return false;
        }

        if (validated is "." or "..")
        {
            error = "Nome inválido.";
            return false;
        }

        if (validated.EndsWith('.') || validated.EndsWith(' '))
        {
            error = "Nome não pode terminar com ponto ou espaço.";
            return false;
        }

        if (validated.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            error = "Nome contém caracteres inválidos.";
            return false;
        }

        if (validated.Contains(Path.DirectorySeparatorChar) || validated.Contains(Path.AltDirectorySeparatorChar))
        {
            error = "Nome não pode conter separadores de pasta.";
            return false;
        }

        return true;
    }

    private static bool IsAncestor(WorkspaceTreeNodeViewModel node, WorkspaceTreeNodeViewModel potentialDescendant)
    {
        var current = potentialDescendant;
        while (current.Parent is not null)
        {
            if (ReferenceEquals(current.Parent, node))
                return true;

            current = current.Parent;
        }

        return false;
    }

    private static void DeletePath(string path)
    {
        if (File.Exists(path))
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
            return;
        }

        if (Directory.Exists(path))
        {
            var directoryInfo = new DirectoryInfo(path);
            foreach (var info in directoryInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            directoryInfo.Delete(recursive: true);
        }
    }

    private static void SearchDirectory(
        string directoryPath,
        string query,
        StringComparison comparison,
        WorkspaceSearchFilter filter,
        List<WorkspaceSearchResultViewModel> results,
        int maxResults)
    {
        if (results.Count >= maxResults)
            return;

        var directoryName = Path.GetFileName(directoryPath);
        if (filter is WorkspaceSearchFilter.All or WorkspaceSearchFilter.Folders)
        {
            var dirMatchIndex = directoryName.IndexOf(query, comparison);
            if (dirMatchIndex >= 0)
            {
                SplitMatch(directoryName, dirMatchIndex, query.Length, out var before, out var match, out var after);
                results.Add(new WorkspaceSearchResultViewModel(
                    WorkspaceSearchResultKind.FolderName,
                    fullPath: directoryPath,
                    fileName: directoryName,
                    before,
                    match,
                    after));
            }
        }

        foreach (var dir in SafeEnumerateDirectories(directoryPath, recursive: false))
        {
            if (results.Count >= maxResults)
                return;

            SearchDirectory(dir, query, comparison, filter, results, maxResults);
        }

        foreach (var file in SafeEnumerateFiles(directoryPath))
        {
            if (results.Count >= maxResults)
                return;

            if (string.Equals(Path.GetFileName(file), WorkspaceStorage.OrderFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            var fileName = Path.GetFileName(file);
            if (filter is WorkspaceSearchFilter.All or WorkspaceSearchFilter.Files)
            {
                var nameIndex = fileName.IndexOf(query, comparison);
                if (nameIndex >= 0)
                {
                    SplitMatch(fileName, nameIndex, query.Length, out var before, out var match, out var after);
                    results.Add(new WorkspaceSearchResultViewModel(
                        WorkspaceSearchResultKind.FileName,
                        fullPath: file,
                        fileName,
                        before,
                        match,
                        after));
                }
            }

            if (filter is not (WorkspaceSearchFilter.All or WorkspaceSearchFilter.Words))
                continue;

            TrySearchFileContents(file, fileName, query, comparison, results, maxResults);
        }
    }

    private static void TrySearchFileContents(
        string filePath,
        string fileName,
        string query,
        StringComparison comparison,
        List<WorkspaceSearchResultViewModel> results,
        int maxResults)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            const long maxBytes = 5 * 1024 * 1024; // 5MB
            if (fileInfo.Length > maxBytes)
                return;

            var lineNumber = 0;
            foreach (var line in File.ReadLines(filePath))
            {
                if (results.Count >= maxResults)
                    return;

                lineNumber++;
                var index = line.IndexOf(query, comparison);
                if (index < 0)
                    continue;

                SplitMatch(line, index, query.Length, out var before, out var match, out var after);

                results.Add(new WorkspaceSearchResultViewModel(
                    WorkspaceSearchResultKind.FileContent,
                    fullPath: filePath,
                    fileName,
                    before,
                    match,
                    after,
                    lineNumber: lineNumber,
                    columnIndex: index));
            }
        }
        catch
        {
            // Ignora arquivos que não são texto / não podem ser lidos.
        }
    }

    private static void SplitMatch(string text, int index, int length, out string before, out string match, out string after)
    {
        var rawBefore = text[..index];
        var rawMatch = text.Substring(index, length);
        var rawAfter = text[(index + length)..];

        const int maxSide = 40;
        before = rawBefore.Length > maxSide ? "..." + rawBefore[^maxSide..] : rawBefore;
        match = rawMatch;
        after = rawAfter.Length > maxSide ? rawAfter[..maxSide] + "..." : rawAfter;
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string directoryPath, bool recursive)
    {
        if (!Directory.Exists(directoryPath))
            yield break;

        IEnumerable<string> dirs;
        try
        {
            dirs = Directory.EnumerateDirectories(directoryPath);
        }
        catch
        {
            yield break;
        }

        foreach (var dir in dirs)
        {
            yield return dir;

            if (!recursive)
                continue;

            foreach (var sub in SafeEnumerateDirectories(dir, recursive: true))
                yield return sub;
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            yield break;

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(directoryPath);
        }
        catch
        {
            yield break;
        }

        foreach (var file in files)
            yield return file;
    }
}
