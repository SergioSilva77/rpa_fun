using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace RpaInventory.App.WorkspaceExplorer;

public partial class WorkspaceFilePreviewWindow : Window
{
    private readonly int? _lineNumber;
    private readonly int? _columnIndex;
    private readonly int _matchLength;

    public WorkspaceFilePreviewWindow(string filePath, int? lineNumber, int? columnIndex, int matchLength)
    {
        InitializeComponent();

        _lineNumber = lineNumber;
        _columnIndex = columnIndex;
        _matchLength = matchLength;

        Title = $"Preview - {Path.GetFileName(filePath)}";
        FilePathText.Text = filePath;

        try
        {
            ContentTextBox.Text = File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            ContentTextBox.Text = $"Erro ao abrir arquivo:\n{ex.Message}";
        }

        Loaded += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ScrollToMatch));
    }

    private void ScrollToMatch()
    {
        if (!_lineNumber.HasValue)
            return;

        var lineIndex = Math.Max(0, _lineNumber.Value - 1);
        ContentTextBox.ScrollToLine(lineIndex);

        if (!_columnIndex.HasValue)
            return;

        var baseIndex = ContentTextBox.GetCharacterIndexFromLineIndex(lineIndex);
        if (baseIndex < 0)
            return;

        var selectStart = baseIndex + Math.Max(0, _columnIndex.Value);
        selectStart = Math.Clamp(selectStart, 0, ContentTextBox.Text.Length);
        var selectLength = Math.Clamp(_matchLength, 0, Math.Max(0, ContentTextBox.Text.Length - selectStart));
        ContentTextBox.Select(selectStart, selectLength);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}

