namespace Waypoint;

/// <summary>One line of the grant cart: a base and how many copies of it a press gives.</summary>
/// <param name="BaseId">The base's metadata path, as /item_give takes it.</param>
/// <param name="Name">The base's name, for the cart's line only.</param>
/// <param name="Count">How many copies.</param>
public sealed record GrantLine(string BaseId, string Name, int Count)
{
    /// <summary>The line as the cart shows it.</summary>
    public string Display => $"{Name} × {Count}";
}

/// <summary>
/// What one press will give: bases added one at a time, merged by base, and bounded in total because every copy is its own
/// /item_give line. Filling it never sends anything; <see cref="Commands"/> is what the window sends.
/// </summary>
public sealed class GrantCart
{
    /// <summary>The most one press may give, across every line.</summary>
    public const int MaximumTotal = 200;

    readonly List<GrantLine> _lines = [];

    /// <summary>The lines in the order they were added.</summary>
    public IReadOnlyList<GrantLine> Lines => _lines;

    /// <summary>How many copies the cart holds.</summary>
    public int Total => _lines.Sum(line => line.Count);

    /// <summary>Adds copies of a base, merging with the line already there; false when the cart cannot take them.</summary>
    public bool Add(string baseId, string name, int count)
    {
        count = BulkGrant.Count(count);
        if (Total + count > MaximumTotal) return false;
        int index = _lines.FindIndex(line => line.BaseId == baseId);
        if (index >= 0) _lines[index] = _lines[index] with { Count = _lines[index].Count + count };
        else _lines.Add(new GrantLine(baseId, name, count));
        return true;
    }

    /// <summary>Takes one line out.</summary>
    public void Remove(GrantLine line) => _lines.Remove(line);

    /// <summary>Empties the cart.</summary>
    public void Clear() => _lines.Clear();

    /// <summary>Every /item_give line one press sends, in the order the cart was filled.</summary>
    public IReadOnlyList<string> Commands(int itemLevel) =>
        [.. _lines.SelectMany(line => BulkGrant.Lines(line.BaseId, itemLevel, line.Count))];
}
