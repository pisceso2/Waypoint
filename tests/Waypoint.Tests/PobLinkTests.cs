using Waypoint;

namespace Waypoint.Tests;

public class PobLinkTests
{
    [Theory]
    [InlineData("https://poe.ninja/poe1/pob/24bcb", "https://poe.ninja/poe1/pob/raw/24bcb")]
    [InlineData("https://poe.ninja/pob/abc123", "https://poe.ninja/poe1/pob/raw/abc123")]
    [InlineData("  https://pobb.in/XYZ-123  ", "https://pobb.in/pob/XYZ-123")]
    [InlineData("https://pob.codes/b/9fA2?x=1", "https://api.pob.codes/9fA2/raw")]
    [InlineData("https://maxroll.gg/poe/pob/q7", "https://maxroll.gg/poe/api/pob/q7")]
    [InlineData("https://pastebin.com/AbCd12", "https://pastebin.com/raw/AbCd12")]
    [InlineData("https://poedb.tw/pob/zz9", "https://poedb.tw/pob/zz9/raw")]
    public void A_known_site_is_resolved_to_its_raw_address(string link, string expected) =>
        Assert.Equal(expected, PobLink.Resolve(link));

    [Theory]
    [InlineData("")]
    [InlineData("eNq9Vk1v2zAM")]
    [InlineData("https://example.com/build")]
    [InlineData("https://poe.ninja/poe1/builds")]
    public void Anything_else_is_not_a_build_link(string text) =>
        Assert.Null(PobLink.Resolve(text));
}
