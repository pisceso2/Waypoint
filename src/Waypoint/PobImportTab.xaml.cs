using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace Waypoint;

/// <summary>
/// PoE import: a Path of Building export code read into what the build is — class, ascendancy, level, what it carries and
/// its passive tree link. Reading only inspects it. Applying a build to a character needs the server's own hooks, which
/// this section does not have, so nothing is written.
/// </summary>
public partial class PobImportTab : UserControl
{
    Shared _shared = null!;
    PobBuild? _build;

    public PobImportTab() => InitializeComponent();

    internal void Attach(Shared shared) => _shared = shared;

    async void ReadClicked(object sender, RoutedEventArgs e)
    {
        string text = Code.Text.Trim();
        ReadButton.IsEnabled = false;
        Summary.Text = L.Text("Reading ...");
        try
        {
            string code = text;
            if (PobLink.Resolve(text) is { } url)
            {
                Summary.Text = L.Text("Downloading the build ...");
                code = await PobLink.DownloadAsync(url);
            }
            _build = await Task.Run(() => PobCode.Read(code));
            Summary.Text = Describe(_build);
            CopyTreeButton.IsEnabled = _build.HasTree;
            CreatePanel.Visibility = Visibility.Visible;
            if (CharacterName.Text.Trim().Length == 0) CharacterName.Text = _build.Class;
        }
        catch (HttpRequestException error)
        {
            _build = null;
            CopyTreeButton.IsEnabled = false;
            Summary.Text = L.Text("Could not download the build: {0}", error.Message);
        }
        catch (TaskCanceledException)
        {
            _build = null;
            CopyTreeButton.IsEnabled = false;
            Summary.Text = L.Text("The site did not answer in time.");
        }
        catch (FormatException error)
        {
            _build = null;
            CopyTreeButton.IsEnabled = false;
            Summary.Text = L.Text("Could not read the build: {0}", error.Message);
        }
        finally { ReadButton.IsEnabled = true; }
    }

    void CopyTreeClicked(object sender, RoutedEventArgs e)
    {
        if (_build?.TreeUrl is not { Length: > 0 } url) return;
        try { Clipboard.SetText(url); Summary.Text = L.Text("Copied the tree link."); }
        catch (System.Runtime.InteropServices.COMException) { Summary.Text = L.Text("Clipboard busy; try again."); }
    }

    /// <summary>Creates a character of the build's class on an account, through the dev host's own create_character.</summary>
    async void CreateClicked(object sender, RoutedEventArgs e)
    {
        if (_build is null) return;
        string account = Account.Text.Trim(), password = Password.Password, name = CharacterName.Text.Trim();
        if (account.Length == 0 || password.Length == 0)
        {
            CreateState.Text = L.Text("Enter the account and its password.");
            return;
        }
        if (!AccountTools.ValidName(name))
        {
            CreateState.Text = L.Text("A name is 3 to 32 letters, digits, _ or -.");
            return;
        }
        if (!AccountList.Classes.Contains(_build.Class, StringComparer.OrdinalIgnoreCase))
        {
            CreateState.Text = L.Text("The dev host cannot create a {0} character.", _build.Class);
            return;
        }
        CreateButton.IsEnabled = false;
        try
        {
            await DevHost.EnsureRunningAsync(line => CreateState.Text = line);
            await DevHost.CreateCharacter(account, password, name, _build.Class);
            CreateState.Text = L.Text("{0} ({1}) created on {2}.", name, _build.Class, account);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.Net.Sockets.SocketException
            or TimeoutException or System.ComponentModel.Win32Exception)
        {
            CreateState.Text = L.Text("Could not create the character: {0}", ex.Message);
        }
        finally { CreateButton.IsEnabled = true; }
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
