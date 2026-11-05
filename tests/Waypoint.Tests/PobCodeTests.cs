using System.IO.Compression;
using System.Text;
using Waypoint;

namespace Waypoint.Tests;

public class PobCodeTests
{
    static string Code(string xml)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize))
            zlib.Write(Encoding.UTF8.GetBytes(xml));
        return Convert.ToBase64String(output.ToArray()).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    const string Sample = """
        <PathOfBuilding>
          <Build level="92" className="Witch" ascendClassName="Occultist" />
          <Tree><Spec treeVersion="3_21"><URL>https://www.pathofexile.com/passive-skill-tree/AAAABgMAAA==</URL></Spec></Tree>
          <Skills><Skill enabled="true" /><Skill enabled="false" /></Skills>
          <Items><Item id="1">Rarity: RARE</Item><Item id="2">Rarity: UNIQUE</Item></Items>
        </PathOfBuilding>
        """;

    [Fact]
    public void A_code_carries_the_class_the_level_the_tree_and_what_it_holds()
    {
        var build = PobCode.Read(Code(Sample));
        Assert.Equal("Witch", build.Class);
        Assert.Equal("Occultist", build.Ascendancy);
        Assert.Equal(92, build.Level);
        Assert.Equal("https://www.pathofexile.com/passive-skill-tree/AAAABgMAAA==", build.TreeUrl);
        Assert.Equal(2, build.Items);
        Assert.Equal(2, build.Skills);
        Assert.True(build.HasTree);
    }

    [Fact]
    public void A_build_without_a_tree_says_so_instead_of_failing()
    {
        var build = PobCode.Read(Code("<PathOfBuilding><Build level=\"1\" className=\"Scion\" /></PathOfBuilding>"));
        Assert.Equal("Scion", build.Class);
        Assert.Equal("", build.Ascendancy);
        Assert.Null(build.TreeUrl);
        Assert.False(build.HasTree);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64 !!")]
    public void A_code_that_is_not_base64_is_refused_with_a_reason(string code) =>
        Assert.Throws<FormatException>(() => PobCode.Decode(code));

    [Fact]
    public void A_document_that_is_not_a_build_is_refused_with_a_reason()
    {
        var error = Assert.Throws<FormatException>(() => PobCode.Read(Code("<Other />")));
        Assert.Contains("Build element", error.Message);
    }

    [Fact]
    public void A_bomb_is_refused_before_it_is_held_in_memory()
    {
        var huge = new string('x', PobCode.MaximumDecodedBytes + 1);
        Assert.Throws<FormatException>(() => PobCode.Decode(Code(huge)));
    }
}
