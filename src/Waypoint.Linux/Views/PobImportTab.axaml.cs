using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Waypoint;

/// <summary>
/// PoE import: a Path of Building export code read into what the build is. Reading only inspects it; applying a build to a
/// character needs the server's own hooks, which this section does not have.
/// </summary>
partial class PobImportTab : UserControl
{
    Shared _shared = null!;
    PobBuild? _build;

    public PobImportTab()
    {
        InitializeComponent();
        ShowLanguage();
    }

    /// <summary>The texts Windows gives through {l:Loc}.</summary>
    void ShowLanguage()
    {
        Hint.Text = L.Text("Paste a Path of Building export code. Reading it only inspects the build: nothing is changed.");
        ReadButton.Content = L.Text("Read build");
        CopyTreeButton.Content = L.Text("Copy tree link");
    }

    public void Attach(Shared shared)
    {
        _shared = shared;
        L.Current.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(ShowLanguage);
    }

    async void ReadClicked(object? sender, RoutedEventArgs e)
    {
        string code = Code.Text ?? "";
        ReadButton.IsEnabled = false;
        Summary.Text = L.Text("Reading ...");
        try
        {
            _build = await Task.Run(() => PobCode.Read(code));
            Summary.Text = Describe(_build);
            CopyTreeButton.IsEnabled = _build.HasTree;
        }
        catch (FormatException error)
        {
            _build = null;
            CopyTreeButton.IsEnabled = false;
            Summary.Text = L.Text("Could not read the build: {0}", error.Message);
        }
        finally { ReadButton.IsEnabled = true; }
    }

    async void CopyTreeClicked(object? sender, RoutedEventArgs e)
    {
        if (_build?.TreeUrl is not { Length: > 0 } url) return;
        Summary.Text = await ViewTools.Copy(this, url) ? L.Text("Copied the tree link.") : L.Text("Clipboard busy; try again.");
    }

    /// <summary>The build as lines, in the interface language; protocol values stay as the export wrote them.</summary>
    string Describe(PobBuild build)
    {
        var lines = new List<string>
        {
            L.Text("Class: {0}", build.Class.Length > 0 ? build.Class : L.Text("Unknown")),
            L.Text("Ascendancy: {0}", build.Ascendancy.Length > 0 ? build.Ascendancy : L.Text("None")),
            L.Text("Level: {0}", build.Level > 0 ? build.Level.ToString() : L.Text("Unknown")),
            L.Text("Items: {0}, skills: {1}", build.Items, build.Skills),
            build.HasTree ? L.Text("Tree: {0}", build.TreeUrl) : L.Text("No tree link."),
        };
        return string.Join(Environment.NewLine, lines);
    }
}
