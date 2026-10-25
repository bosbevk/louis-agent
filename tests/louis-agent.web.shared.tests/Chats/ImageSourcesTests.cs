using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Web.Shared.Tests.Chats;

// FX22: the images a turn may show: the union of what its blocks' results gave and what every sub-turn's did, recursively (a minion inside a specialist too).
public class ImageSourcesTests
{
    private static Block Tool(params string[] images) => new() { Kind = BlockKind.Tool, ToolName = "FetchUrl", Images = [.. images] };

    private static Block Delegating(Turn sub) => new() { Kind = BlockKind.Tool, ToolName = "StartSubSession", SubTurn = sub };

    [Test]
    public void ATurnWithNoImages_HasNone() => Assert.That(ImageSources.Of(new Turn()), Is.Empty);

    [Test]
    public void ItIsTheUnionOfTheTurnsBlocks_AndEverySubTurnsRecursively()
    {
        var minion = new Turn { Blocks = { Tool("https://c.example/3.jpg") } };
        var specialist = new Turn { Blocks = { Tool("https://b.example/2.jpg", "https://a.example/1.jpg"), Delegating(minion) } };
        var turn = new Turn { Blocks = { Tool("https://a.example/1.jpg"), Delegating(specialist) } };

        Assert.That(ImageSources.Of(turn), Is.EquivalentTo(new[] { "https://a.example/1.jpg", "https://b.example/2.jpg", "https://c.example/3.jpg" }));
    }

    [Test]
    public void ItIsPerTurn_ALaterTurnCannotReuseAnEarlierOnesImage()
    {
        var earlier = new Turn { Blocks = { Tool("https://a.example/1.jpg") } };
        var later = new Turn();

        Assert.That(ImageSources.Of(earlier), Has.Count.EqualTo(1));
        Assert.That(ImageSources.Of(later), Is.Empty);
    }
}
