using System.Text.Json;
using Waypoint;

namespace Waypoint.Tests;

public class GrantCartTests
{
    [Fact]
    public void The_same_base_merges_into_one_line()
    {
        var cart = new GrantCart();
        Assert.True(cart.Add("Metadata/Items/Armours/Gloves/GlovesStr1", "Iron Gauntlets", 3));
        Assert.True(cart.Add("Metadata/Items/Armours/Gloves/GlovesStr1", "Iron Gauntlets", 2));
        var line = Assert.Single(cart.Lines);
        Assert.Equal(5, line.Count);
        Assert.Equal("Iron Gauntlets × 5", line.Display);
        Assert.Equal(5, cart.Total);
    }

    [Fact]
    public void Different_bases_stay_separate_lines()
    {
        var cart = new GrantCart();
        cart.Add("a", "A", 1);
        cart.Add("b", "B", 2);
        Assert.Equal(2, cart.Lines.Count);
        Assert.Equal(3, cart.Total);
    }

    [Fact]
    public void The_bound_counts_every_copy_and_refuses_what_does_not_fit()
    {
        var cart = new GrantCart();
        for (int i = 0; i < GrantCart.MaximumTotal / BulkGrant.MaximumCount; i++)
            Assert.True(cart.Add($"base{i}", $"Base {i}", BulkGrant.MaximumCount));
        Assert.Equal(GrantCart.MaximumTotal, cart.Total);
        Assert.False(cart.Add("one-more", "One more", 1));
        Assert.Equal(GrantCart.MaximumTotal, cart.Total);
    }

    [Fact]
    public void Removing_and_clearing_take_copies_out()
    {
        var cart = new GrantCart();
        cart.Add("a", "A", 2);
        cart.Add("b", "B", 1);
        cart.Remove(cart.Lines[0]);
        Assert.Equal(1, cart.Total);
        cart.Clear();
        Assert.Empty(cart.Lines);
    }

    [Fact]
    public void Every_copy_becomes_its_own_give_line_in_cart_order()
    {
        var cart = new GrantCart();
        cart.Add("b", "B", 2);
        cart.Add("a", "A", 1);
        var commands = cart.Commands(itemLevel: 84);
        Assert.Equal(3, commands.Count);
        Assert.All(commands, command => Assert.StartsWith("/item_give {", command));
        using var first = JsonDocument.Parse(commands[0]["/item_give ".Length..]);
        Assert.Equal("b", first.RootElement.GetProperty("base").GetString());
        using var third = JsonDocument.Parse(commands[2]["/item_give ".Length..]);
        Assert.Equal("a", third.RootElement.GetProperty("base").GetString());
    }
}
