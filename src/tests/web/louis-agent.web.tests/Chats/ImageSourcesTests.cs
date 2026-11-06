using LouisAgent.Web.Chats;

namespace LouisAgent.Web.Tests.Chats;

/// <summary>
/// The images a turn may show: the union of what its blocks' results gave and what every sub-turn's did, recursively.
/// </summary>
public class ImageSourcesTests
{
    /// <summary>A tool call whose result gave these images.</summary>
    private static Block Tool(params string[] images) => new() { Kind = BlockKind.Tool, ToolName = "FetchUrl", Images = [.. images] };

    /// <summary>A delegation call holding this sub-turn.</summary>
    private static Block Delegating(Turn sub) => new() { Kind = BlockKind.Tool, ToolName = "StartSubSession", SubTurn = sub };

    /// <summary>A turn with no images has none.</summary>
    [Test]
    public void ATurnWithNoImages_HasNone() => Assert.That(ImageSources.Of(new Turn()), Is.Empty);

    /// <summary>It's the union of the turn's blocks and every sub-turn's, all the way down.</summary>
    [Test]
    public void ItIsTheUnionOfTheTurnsBlocks_AndEverySubTurnsRecursively()
    {
        var minion = new Turn { Blocks = { Tool("https://c.example/3.jpg") } };
        var specialist = new Turn { Blocks = { Tool("https://b.example/2.jpg", "https://a.example/1.jpg"), Delegating(minion) } };
        var turn = new Turn { Blocks = { Tool("https://a.example/1.jpg"), Delegating(specialist) } };

        Assert.That(ImageSources.Of(turn), Is.EquivalentTo(new[] { "https://a.example/1.jpg", "https://b.example/2.jpg", "https://c.example/3.jpg" }));
    }

    /// <summary>It's per turn: a later turn can't reuse an earlier one's image.</summary>
    [Test]
    public void ItIsPerTurn_ALaterTurnCannotReuseAnEarlierOnesImage()
    {
        var earlier = new Turn { Blocks = { Tool("https://a.example/1.jpg") } };
        var later = new Turn();

        Assert.That(ImageSources.Of(earlier), Has.Count.EqualTo(1));
        Assert.That(ImageSources.Of(later), Is.Empty);
    }
}
