using System.Text.Json;
using System.Text.Json.Serialization;

namespace Waypoint;

/// <summary>
/// The lines one bulk grant sends: /item_give, once per copy. Each line is a round trip to the client, so a press is
/// bounded rather than unbounded, and the window reports how far it got. Nothing here talks to the Gateway: the argument
/// is the same JSON the item crafter already sends.
/// </summary>
public static class BulkGrant
{
    /// <summary>The most one press may send; every copy is its own line through the bridge.</summary>
    public const int MaximumCount = 50;

    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    /// <summary>The count a request may send, kept between one and <see cref="MaximumCount"/>.</summary>
    public static int Count(int requested) => Math.Clamp(requested, 1, MaximumCount);

    /// <summary>The argument /item_give takes for a plain copy of a base at an item level.</summary>
    public static string Spec(string baseId, int itemLevel, int quality = 0) =>
        JsonSerializer.Serialize(new
        {
            @base = baseId,
            rarity = "normal",
            itemLevel,
            quality = quality > 0 ? quality : (int?)null,
        }, Json);

    /// <summary>The lines one grant sends, all for the same base.</summary>
    public static IReadOnlyList<string> Lines(string baseId, int itemLevel, int count, int quality = 0)
    {
        var line = "/item_give " + Spec(baseId, itemLevel, quality);
        return [.. Enumerable.Repeat(line, Count(count))];
    }
}
