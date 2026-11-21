using LouisAgent.Diagrams.Engine.Keys;

namespace LouisAgent.Diagrams.Tests.Keys;

/// <summary>
/// CL65.2: a key's items are placed by one class. Four kinds each had the same loop with the same five numbers: the
/// items side by side, each after the one before, and on to a new row at the left when the width is full.
/// </summary>
public class KeyRowsTests
{
    /// <summary>An item of a key, for these tests: how wide its sample is, its words, and where it was put.</summary>
    private sealed record Item(double Sample, string Words, double X = -1, double Y = -1);

    /// <summary>The items placed across a width.</summary>
    private static (IReadOnlyList<Item> Items, double Height) Placed(double width, params Item[] items) =>
        KeyRows.Place(items, item => item.Sample, item => item.Words, (item, x, y) => item with { X = x, Y = y },
            width);

    /// <summary>
    /// The first item stands at the left, under the heading's room. The next starts after the first's sample, the
    /// room before its words, its words at 6.6 a character, and 30 between two items.
    /// </summary>
    [Test]
    public void Items_StandSideBySide_EachAfterTheOneBefore()
    {
        var (items, height) = Placed(1000, new Item(18, "starts it"), new Item(30, "a decision"));

        Assert.Multiple(() =>
        {
            Assert.That((items[0].X, items[0].Y), Is.EqualTo((0.0, KeyRows.Heading)));
            Assert.That(items[1].X, Is.EqualTo(18 + KeyRows.Words + 9 * 6.6 + 30).Within(0.001));
            Assert.That(items[1].Y, Is.EqualTo(KeyRows.Heading));
            Assert.That(height, Is.EqualTo(KeyRows.Heading + KeyRows.Row), "one row under the heading");
        });
    }

    /// <summary>
    /// An item that would pass the width goes on to a new row, at the left, and the key is a row taller. An item
    /// wider than the whole width still stands at the left of a row, and is not pushed down for ever.
    /// </summary>
    [Test]
    public void AnItemPastTheWidth_GoesOnToANewRow_AtTheLeft()
    {
        var (items, height) = Placed(200, new Item(18, "starts it"), new Item(30, "a decision"),
            new Item(30, new string('w', 80)));

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.X), Is.All.Zero);
            Assert.That(items.Select(item => item.Y), Is.EqualTo(new[]
            {
                KeyRows.Heading, KeyRows.Heading + KeyRows.Row, KeyRows.Heading + 2 * KeyRows.Row
            }));
            Assert.That(height, Is.EqualTo(KeyRows.Heading + 3 * KeyRows.Row));
        });
    }

    /// <summary>A key with no item is its heading and one empty row, as each kind's key was.</summary>
    [Test]
    public void AKeyWithNoItem_IsItsHeadingAndOneRow() =>
        Assert.That(Placed(300).Height, Is.EqualTo(KeyRows.Heading + KeyRows.Row));
}
