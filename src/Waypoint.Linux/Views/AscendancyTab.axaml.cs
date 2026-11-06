using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Waypoint;

/// <summary>One character as the ascendancy page shows it: the account it belongs to and what the account service says.</summary>
public sealed record AscendancyRow(string Account, string Name, string Class, string Ascendancy, int Level);

/// <summary>
/// What each character is: its account, name, class, ascendancy and level, read from the account service. Switching an
/// ascendancy needs a command the server does not offer, so nothing here is written.
/// </summary>
partial class AscendancyTab : UserControl
{
    Shared _shared = null!;

    public AscendancyTab()
    {
        InitializeComponent();
        ShowLanguage();
    }

    /// <summary>The texts Windows gives through {l:Loc}.</summary>
    void ShowLanguage()
    {
        Hint.Text = L.Text("What each character is, from the account service; the switch below acts on the character in game.");
        RefreshButton.Content = L.Text("Refresh");
        SwitchTitle.Text = L.Text("Switch the character in game");
        SwitchHint.Text = L.Text("These act on the character in game and change its saved ascendancy: list the choices, pick one, then switch.");
        ListButton.Content = L.Text("List the choices");
        SwitchButton.Content = L.Text("Switch");
    }

    public void Attach(Shared shared)
    {
        _shared = shared;
        L.Current.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(ShowLanguage);
    }

    /// <summary>What IsVisibleChanged does on Windows: the table is read the first time the page shows.</summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty && IsVisible && _shared is not null) _ = LoadAsync();
    }

    async void RefreshClicked(object? sender, RoutedEventArgs e) => await LoadAsync();

    /// <summary>Asks the Gateway for the ascendancies this character may take: it answers with the exact lines to send.</summary>
    async void ListClicked(object? sender, RoutedEventArgs e)
    {
        if (Repo.IsInstall)
        {
            SwitchState.Text = L.Text("A release install has no client control bridge.");
            return;
        }
        SwitchState.Text = L.Text("Asking the Gateway ...");
        try
        {
            var (_, lines) = ClientBridge.Replies(await _shared.Bridge.SendAsync("/ascendancy"));
            var choices = lines.Select(line => line.Trim()).Where(line => line.StartsWith("/ascendancy ", StringComparison.Ordinal)).ToList();
            Choices.ItemsSource = choices;
            SwitchState.Text = choices.Count > 0 ? L.Text("{0} choices.", choices.Count) : string.Join(" ", lines);
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or TimeoutException
            or System.ComponentModel.Win32Exception)
        {
            SwitchState.Text = L.Text("Could not ask the Gateway: {0}", error.Message);
        }
    }

    /// <summary>Sends the chosen line: the Gateway switches the character's saved ascendancy itself.</summary>
    async void SwitchClicked(object? sender, RoutedEventArgs e)
    {
        if (Choices.SelectedItem is not string line)
        {
            SwitchState.Text = L.Text("List the choices first, then pick one.");
            return;
        }
        SwitchState.Text = L.Text("Switching ...");
        try
        {
            var (_, lines) = ClientBridge.Replies(await _shared.Bridge.SendAsync(line));
            SwitchState.Text = lines.Count > 0 ? string.Join(" ", lines) : L.Text("Sent {0}.", line);
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or TimeoutException
            or System.ComponentModel.Win32Exception)
        {
            SwitchState.Text = L.Text("Could not switch: {0}", error.Message);
        }
    }

    async Task LoadAsync()
    {
        State.Text = L.Text("Reading the accounts ...");
        var (inventory, problem) = await _shared.Workspace.ReadAccountsAsync();
        if (inventory is null)
        {
            Table.ItemsSource = null;
            State.Text = L.Text("Could not read the accounts: {0}", problem ?? "");
            return;
        }
        var rows = inventory.Accounts
            .SelectMany(account => account.Characters.Select(character =>
                new AscendancyRow(account.Name, character.Name, character.Class, character.Ascendancy, character.Level)))
            .ToList();
        Table.ItemsSource = rows;
        State.Text = rows.Count == 0 ? L.Text("No characters yet.") : L.Text("{0} character(s).", rows.Count);
    }
}
