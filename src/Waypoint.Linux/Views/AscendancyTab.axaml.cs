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
        Hint.Text = L.Text("What each character is, from the account service. Switching an ascendancy needs a command the server does not offer yet, so this page only reads.");
        RefreshButton.Content = L.Text("Refresh");
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
