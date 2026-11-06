using System.Text.Json;
using Waypoint;

namespace Waypoint.Tests;

public class BulkGrantTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    [InlineData(999, BulkGrant.MaximumCount)]
    [InlineData(-5, 1)]
    public void A_count_is_kept_between_one_and_the_bound(int requested, int expected) =>
        Assert.Equal(expected, BulkGrant.Count(requested));

    [Fact]
    public void The_argument_names_the_base_and_leaves_out_a_zero_quality()
    {
        using var plain = JsonDocument.Parse(BulkGrant.Spec("Metadata/Items/Armours/Gloves/Gloves1", 84));
        Assert.Equal("Metadata/Items/Armours/Gloves/Gloves1", plain.RootElement.GetProperty("base").GetString());
        Assert.Equal("normal", plain.RootElement.GetProperty("rarity").GetString());
        Assert.Equal(84, plain.RootElement.GetProperty("itemLevel").GetInt32());
        Assert.False(plain.RootElement.TryGetProperty("quality", out _));

        using var quality = JsonDocument.Parse(BulkGrant.Spec("Metadata/Items/Armours/Gloves/Gloves1", 84, quality: 20));
        Assert.Equal(20, quality.RootElement.GetProperty("quality").GetInt32());
    }

    [Fact]
    public void One_line_per_copy_and_each_one_is_a_give_command()
    {
        var lines = BulkGrant.Lines("Metadata/Items/Armours/Gloves/Gloves1", 84, 3);
        Assert.Equal(3, lines.Count);
        Assert.All(lines, line => Assert.StartsWith("/item_give {", line));
        Assert.Single(lines.Distinct());
    }
}
