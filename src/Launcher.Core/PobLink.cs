using System.Net.Http;
using System.Text.RegularExpressions;

namespace Waypoint;

/// <summary>
/// A pasted build link resolved to the place that serves the raw Path of Building code. The sites and their raw addresses
/// are the ones Path of Building itself uses (<c>BuildSiteTools.lua</c>), so a link that works there works here: poe.ninja,
/// pobb.in, pob.codes, Maxroll, Pastebin and poedb.
/// </summary>
public static partial class PobLink
{
    /// <summary>The user agent the sites see; a build link is a small, one-off request.</summary>
    const string Agent = "Waypoint";

    /// <summary>How long one download may take before the import gives up.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    /// <summary>The address that serves the raw code for a link, or null when the link is not one of the known sites.</summary>
    public static string? Resolve(string? text)
    {
        var link = (text ?? "").Trim();
        if (link.Length == 0 || !link.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return null;
        foreach (var (pattern, raw) in Sites)
        {
            var match = pattern.Match(link);
            if (match.Success) return raw(match.Groups["id"].Value);
        }
        return null;
    }

    /// <summary>The raw code a link serves.</summary>
    public static async Task<string> DownloadAsync(string url, CancellationToken stop = default)
    {
        using var http = new HttpClient { Timeout = Timeout };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(Agent);
        var code = await http.GetStringAsync(url, stop);
        if (code.Trim().Length == 0) throw new FormatException("the link served an empty build");
        return code.Trim();
    }

    /// <summary>The sites Path of Building lists, with the address each serves its raw code from.</summary>
    static readonly (Regex Pattern, Func<string, string> Raw)[] Sites =
    [
        (PoeNinja(), id => $"https://poe.ninja/poe1/pob/raw/{id}"),
        (Pobbin(), id => $"https://pobb.in/pob/{id}"),
        (PobCodes(), id => $"https://api.pob.codes/{id}/raw"),
        (Maxroll(), id => $"https://maxroll.gg/poe/api/pob/{id}"),
        (Pastebin(), id => $"https://pastebin.com/raw/{id}"),
        (Poedb(), id => $"https://poedb.tw/pob/{id}/raw"),
    ];

    [GeneratedRegex(@"^https?://(?:www\.)?poe\.ninja/(?:poe1/)?pob/(?<id>\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex PoeNinja();

    [GeneratedRegex(@"^https?://(?:www\.)?pobb\.in/(?<id>[^?\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex Pobbin();

    [GeneratedRegex(@"^https?://(?:www\.)?pob\.codes/b/(?<id>[^?\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PobCodes();

    [GeneratedRegex(@"^https?://(?:www\.)?maxroll\.gg/poe/pob/(?<id>[^?\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex Maxroll();

    [GeneratedRegex(@"^https?://(?:www\.)?pastebin\.com/(?<id>\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex Pastebin();

    [GeneratedRegex(@"^https?://(?:www\.)?poedb\.tw/pob/(?<id>[^?\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex Poedb();
}
