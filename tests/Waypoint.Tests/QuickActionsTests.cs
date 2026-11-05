using Waypoint;

namespace Waypoint.Tests;

public class QuickActionsTests
{
    [Fact]
    public void A_name_is_trimmed_to_one_line_and_bounded()
    {
        Assert.Equal("Ann", QuickActions.Clean("  Ann  "));
        Assert.Equal("Ann", QuickActions.Clean("An\tn"));
        Assert.Equal(32, QuickActions.Clean(new string('a', 40)).Length);
        Assert.Equal("", QuickActions.Clean(null));
    }

    [Fact]
    public void A_party_line_needs_a_name_and_others_do_not()
    {
        var all = QuickActions.Groups.SelectMany(g => g.Actions).ToList();
        var invite = all.Single(a => a.Label == "Invite");
        Assert.Null(QuickActions.Line(invite, "   "));
        Assert.Equal("/invite Ann", QuickActions.Line(invite, " Ann "));

        var hideout = all.Single(a => a.Label == "Hideout");
        Assert.Equal("/hideout", QuickActions.Line(hideout, null));
    }

    [Fact]
    public void A_typed_line_needs_its_slash_and_some_text()
    {
        Assert.Equal("/hideout", QuickActions.Custom(" /hideout "));
        Assert.Null(QuickActions.Custom("hideout"));
        Assert.Null(QuickActions.Custom("/"));
        Assert.Null(QuickActions.Custom(""));
    }

    [Fact]
    public void Every_built_in_line_is_a_chat_command()
    {
        foreach (var (_, actions) in QuickActions.Groups)
        foreach (var action in actions)
            Assert.StartsWith("/", action.Command);
    }
}
