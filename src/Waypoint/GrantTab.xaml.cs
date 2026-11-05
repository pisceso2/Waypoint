using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Waypoint;

/// <summary>
/// One-click grant: bases from the Gateway's own catalog, filtered by class and name, added to a cart with a copy count
/// each, and given in one press. Every copy is one /item_give line through the client control bridge, the path the item
/// crafter already uses; the cart is bounded and the state line reports how far a press got.
/// </summary>
public partial class GrantTab : UserControl
{
    Shared _shared = null!;
    readonly GrantCart _cart = new();
    IReadOnlyList<CraftBase> _bases = [];
    bool _giving;

    public GrantTab() => InitializeComponent();

    internal void Attach(Shared shared)
    {
        _shared = shared;
        if (Repo.IsInstall)
        {
            AddButton.IsEnabled = false;
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
            ShowCategories();
            ShowBases();
            State.Text = L.Text("{0} bases.", _bases.Count);
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or TimeoutException
            or System.ComponentModel.Win32Exception or System.Text.Json.JsonException)
        {
            State.Text = L.Text("Could not load the item bases: {0}", e.Message);
        }
    }

    /// <summary>The class picker: every class the catalog has, under "All".</summary>
    void ShowCategories()
    {
        CategoryPicker.Items.Clear();
        CategoryPicker.Items.Add(new ComboBoxItem { Content = L.Text("All"), Tag = "" });
        foreach (var name in _bases.Select(b => b.Class).Distinct().OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
            CategoryPicker.Items.Add(new ComboBoxItem { Content = name, Tag = name });
        CategoryPicker.SelectedIndex = 0;
    }

    string Category => CategoryPicker.SelectedItem is ComboBoxItem { Tag: string tag } ? tag : "";

    void FilterChanged(object sender, SelectionChangedEventArgs e) => ShowBases();

    void FilterTextChanged(object sender, TextChangedEventArgs e) => ShowBases();

    void ShowBases()
    {
        var text = (Filter.Text ?? "").Trim();
        var category = Category;
        Bases.ItemsSource = _bases
            .Where(b => category.Length == 0 || b.Class == category)
            .Where(b => text.Length == 0 || b.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    void AddClicked(object sender, RoutedEventArgs e)
    {
        if (Bases.SelectedItem is not CraftBase basis)
        {
            State.Text = L.Text("Choose an item base first.");
            return;
        }
        int count = int.TryParse(CountBox.Text, out int asked) ? asked : 1;
        if (!_cart.Add(basis.Id, basis.Name, count))
        {
            State.Text = L.Text("The cart holds at most {0} copies.", GrantCart.MaximumTotal);
            return;
        }
        ShowCart();
        State.Text = L.Text("Added {0} × {1}.", basis.Name, BulkGrant.Count(count));
    }

    void ShowCart() => Cart.ItemsSource = _cart.Lines.ToList();

    void ClearClicked(object sender, RoutedEventArgs e)
    {
        _cart.Clear();
        ShowCart();
        State.Text = L.Text("The cart is empty.");
    }

    async void GiveClicked(object sender, RoutedEventArgs e)
    {
        if (_giving) return;
        if (_cart.Total == 0)
        {
            State.Text = L.Text("Add something to the cart first.");
            return;
        }
        if (!int.TryParse(ItemLevelBox.Text, out int level) || level < 1)
        {
            State.Text = L.Text("Type an item level.");
            return;
        }
        var commands = _cart.Commands(level);
        _giving = true;
        GiveButton.IsEnabled = false;
        try
        {
            for (int i = 0; i < commands.Count; i++)
            {
                State.Text = L.Text("Giving {0} of {1} ...", i + 1, commands.Count);
                await _shared.Bridge.SendAsync(commands[i]);
            }
            State.Text = L.Text("Given {0} copies.", commands.Count);
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
