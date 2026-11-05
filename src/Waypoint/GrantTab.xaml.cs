using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Waypoint;

/// <summary>
/// One-click grant: a base from the Gateway's own crafter catalog, given as plain copies. Each copy is one /item_give line
/// through the client control bridge, the path the item crafter already uses; a press is bounded by
/// <see cref="BulkGrant.MaximumCount"/> and reports how far it got.
/// </summary>
public partial class GrantTab : UserControl
{
    Shared _shared = null!;
    IReadOnlyList<CraftBase> _bases = [];
    bool _giving;

    public GrantTab() => InitializeComponent();

    internal void Attach(Shared shared)
    {
        _shared = shared;
        if (Repo.IsInstall)
        {
            GiveButton.IsEnabled = false;
            State.Text = L.Text("A release install has no item catalog; a checkout with the crafter does.");
            return;
        }
        _ = LoadAsync();
    }

    async Task LoadAsync()
    {
        State.Text = L.Text("Loading item bases ...");
        try
        {
            var catalog = await CrafterCatalog.LoadAsync();
            _bases = catalog.Bases;
            Show(Filter.Text);
            State.Text = L.Text("{0} bases.", _bases.Count);
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or TimeoutException
            or System.ComponentModel.Win32Exception or System.Text.Json.JsonException)
        {
            State.Text = L.Text("Could not load the item bases: {0}", e.Message);
        }
    }

    void FilterChanged(object sender, TextChangedEventArgs e) => Show(Filter.Text);

    void Show(string? filter)
    {
        var text = (filter ?? "").Trim();
        Bases.ItemsSource = text.Length == 0
            ? _bases
            : _bases.Where(b => b.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                || b.Class.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    async void GiveClicked(object sender, RoutedEventArgs e)
    {
        if (_giving) return;
        if (Bases.SelectedItem is not CraftBase basis)
        {
            State.Text = L.Text("Choose an item base first.");
            return;
        }
        if (!int.TryParse(ItemLevelBox.Text, out int level) || level < 1)
        {
            State.Text = L.Text("Type an item level.");
            return;
        }
        var lines = BulkGrant.Lines(basis.Id, level, int.TryParse(CountBox.Text, out int asked) ? asked : 1);
        _giving = true;
        GiveButton.IsEnabled = false;
        try
        {
            for (int i = 0; i < lines.Count; i++)
            {
                State.Text = L.Text("Giving {0} of {1} ...", i + 1, lines.Count);
                await _shared.Bridge.SendAsync(lines[i]);
            }
            State.Text = L.Text("Given {0} of {1}.", lines.Count, basis.Name);
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or TimeoutException
            or System.ComponentModel.Win32Exception)
        {
            State.Text = L.Text("Could not give: {0}", error.Message);
        }
        finally
        {
            _giving = false;
            GiveButton.IsEnabled = true;
        }
    }
}
