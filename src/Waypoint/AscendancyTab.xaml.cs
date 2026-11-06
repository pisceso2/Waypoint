using System.Windows;
using System.Windows.Controls;

namespace Waypoint;

/// <summary>One character as the ascendancy page shows it: the account it belongs to and what the account service says.</summary>
public sealed record AscendancyRow(string Account, string Name, string Class, string Ascendancy, int Level);

/// <summary>
/// What each character is: its account, name, class, ascendancy and level, read from the account service. Switching an
/// ascendancy needs a command the server does not offer, so nothing here is written.
/// </summary>
public partial class AscendancyTab : UserControl
{
    Shared _shared = null!;

    public AscendancyTab() => InitializeComponent();

    internal void Attach(Shared shared)
    {
        _shared = shared;
        IsVisibleChanged += async (_, e) => { if ((bool)e.NewValue) await LoadAsync(); };
    }

    async void RefreshClicked(object sender, RoutedEventArgs e) => await LoadAsync();

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
