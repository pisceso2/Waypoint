using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Waypoint;

/// <summary>A build read from a Path of Building export code: what the character is and what it carries.</summary>
/// <param name="Class">The character's class, as the export names it, or an empty string.</param>
/// <param name="Ascendancy">The ascendancy, or an empty string for a character that has none yet.</param>
/// <param name="Level">The level the export was made at, or 0 when it does not say.</param>
/// <param name="TreeUrl">The passive tree link, or null when the export has none.</param>
/// <param name="Items">How many items the export carries.</param>
/// <param name="Skills">How many skill groups the export carries.</param>
public sealed record PobBuild(string Class, string Ascendancy, int Level, string? TreeUrl, int Items, int Skills)
{
    /// <summary>Whether the export points at a passive tree.</summary>
    public bool HasTree => !string.IsNullOrEmpty(TreeUrl);
}

/// <summary>
/// Path of Building export codes: URL-safe base64 of a zlib-compressed XML document. Decoding is bounded and every
/// failure is a <see cref="FormatException"/> with a reason, so the window can tell the player what is wrong instead of
/// showing a stack trace. Nothing here talks to the game: reading a build never changes anything.
/// </summary>
public static class PobCode
{
    /// <summary>The most a code may decompress to; a real build is far smaller and a bomb is refused.</summary>
    public const int MaximumDecodedBytes = 8 * 1024 * 1024;

    /// <summary>The XML a code carries.</summary>
    public static string Decode(string? code)
    {
        var text = (code ?? "").Trim();
        if (text.Length == 0) throw new FormatException("the code is empty");
        text = text.Replace('-', '+').Replace('_', '/');
        text = text.PadRight(text.Length + ((4 - (text.Length % 4)) % 4), '=');
        byte[] compressed;
        try { compressed = Convert.FromBase64String(text); }
        catch (FormatException) { throw new FormatException("the code is not base64"); }

        using var input = new MemoryStream(compressed);
        using var inflate = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int read;
        while ((read = inflate.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, read);
            if (output.Length > MaximumDecodedBytes) throw new FormatException("the build is too large");
        }
        return Encoding.UTF8.GetString(output.ToArray());
    }

    /// <summary>The build one decoded document describes.</summary>
    public static PobBuild Parse(string xml)
    {
        XDocument document;
        try { document = XDocument.Parse(xml); }
        catch (XmlException) { throw new FormatException("the build is not XML"); }
        var build = document.Root?.Element("Build") ?? throw new FormatException("the build has no Build element");
        var level = int.TryParse(Attribute(build, "level"), out var parsed) ? parsed : 0;
        var tree = document.Root?.Element("Tree")?
            .Elements("Spec")
            .Select(spec => spec.Element("URL")?.Value?.Trim())
            .FirstOrDefault(url => !string.IsNullOrEmpty(url));
        return new PobBuild(
            Attribute(build, "className"),
            Attribute(build, "ascendClassName"),
            level,
            tree,
            document.Root?.Element("Items")?.Elements("Item").Count() ?? 0,
            document.Root?.Element("Skills")?.Elements("Skill").Count() ?? 0);
    }

    /// <summary>The build a code carries.</summary>
    public static PobBuild Read(string? code) => Parse(Decode(code));

    static string Attribute(XElement element, string name) => (string?)element.Attribute(name) ?? "";
}
