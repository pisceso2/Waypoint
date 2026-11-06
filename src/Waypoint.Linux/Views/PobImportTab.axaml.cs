using System.Net.Http;
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
        Hint.Text = L.Text("Paste a Path of Building export code or a build link from poe.ninja, pobb.in, pob.codes, Maxroll, Pastebin or poedb. Reading it only inspects the build: nothing is changed.");
        ReadButton.Content = L.Text("Read build");
        CopyTreeButton.Content = L.Text("Copy tree link");
        CreateLabel.Text = L.Text("Create a character from this build");
        AccountLabel.Text = L.Text("Account");
        PasswordLabel.Text = L.Text("Password");
        NameLabel.Text = L.Text("Name");
        CreateButton.Content = L.Text("Create character");
    }

    public void Attach(Shared shared)
    {
        _shared = shared;
        L.Current.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(ShowLanguage);
    }

    async void ReadClicked(object? sender, RoutedEventArgs e)
    {
        string text = (Code.Text ?? "").Trim();
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
            CreatePanel.IsVisible = true;
            if ((CharacterName.Text ?? "").Trim().Length == 0) CharacterName.Text = _build.Class;
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

    async void CopyTreeClicked(object? sender, RoutedEventArgs e)
    {
        if (_build?.TreeUrl is not { Length: > 0 } url) return;
        Summary.Text = await ViewTools.Copy(this, url) ? L.Text("Copied the tree link.") : L.Text("Clipboard busy; try again.");
    }

    /// <summary>Creates a character of the build's class on an account, through the dev host's own create_character.</summary>
    async void CreateClicked(object? sender, RoutedEventArgs e)
    {
        if (_build is null) return;
        string account = (Account.Text ?? "").Trim(), password = Password.Text ?? "", name = (CharacterName.Text ?? "").Trim();
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
            build.Skills > 0 ? L.Text("Items: {0}, skills: {1}", build.Items, build.Skills) : L.Text("Items: {0}", build.Items),
            build.HasTree ? L.Text("Tree: {0}", build.TreeUrl) : L.Text("No tree link."),
        };
        return string.Join(Environment.NewLine, lines);
    }
}
