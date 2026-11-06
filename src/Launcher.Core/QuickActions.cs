namespace Waypoint;

/// <summary>One button of the Quick functions: the line it sends to the game's chat, and whether it takes the player's
/// name.</summary>
/// <param name="Label">The English source of the button's text; the view translates it.</param>
/// <param name="Command">The chat line, with <c>{0}</c> where the player's name goes.</param>
/// <param name="TakesPlayer">Whether the line needs the name typed beside it.</param>
public sealed record QuickAction(string Label, string Command, bool TakesPlayer = false);

/// <summary>
/// The lines the Quick functions send: plain chat lines, handed to the client through the control bridge exactly as if
/// they were typed into the chat box, so the Gateway runs them the way it runs one typed there. Nothing here talks to the
/// Gateway directly, and a release install has no bridge: its buttons copy the line for the chat instead.
/// </summary>
public static class QuickActions
{
    /// <summary>The groups of buttons, in the order the panel shows them.</summary>
    public static readonly (string Group, QuickAction[] Actions)[] Groups =
    [
        ("Travel", [new("Hideout", "/hideout")]),
        ("Party",
        [
            new("Invite", "/invite {0}", TakesPlayer: true),
            new("Kick", "/kick {0}", TakesPlayer: true),
            new("Trade", "/trade {0}", TakesPlayer: true),
            new("Leave party", "/leave"),
        ]),
        ("Status",
        [
            new("Do not disturb", "/dnd"),
            new("Away", "/afk"),
            new("Remaining", "/remaining"),
        ]),
    ];

    /// <summary>A chat line as it may be sent: trimmed, one line, at most <paramref name="maximum"/> characters.</summary>
    public static string Clean(string? text, int maximum = 32)
    {
        var trimmed = (text ?? "").Trim();
        if (trimmed.Length > maximum) trimmed = trimmed[..maximum];
        return new string([.. trimmed.Where(c => !char.IsControl(c))]);
    }

    /// <summary>The line one action sends, or null when the name it needs is missing.</summary>
    public static string? Line(QuickAction action, string? player)
    {
        if (!action.TakesPlayer) return action.Command;
        var name = Clean(player);
        return name.Length == 0 ? null : action.Command.Replace("{0}", name, StringComparison.Ordinal);
    }

    /// <summary>A line typed by the player as it may be sent: one chat command, not empty and not without its slash.</summary>
    public static string? Custom(string? text)
    {
        var line = Clean(text, 200);
        return line.StartsWith('/') && line.Length > 1 ? line : null;
    }
}
