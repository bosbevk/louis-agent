using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Engine.Classes.Keys;
using LouisAgent.Diagrams.Engine.Classes.Marks;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Classes.Keys;

/// <summary>
/// A class chart's key (F134.11): "how to read it", then an item for each tone its badges wear, each letter its
/// badges show and each kind of link it has, only where the picture has one, side by side and on to a new row.
/// </summary>
public class ClassKeyLayoutTests
{
    /// <summary>An interface, the static class that implements it, and a record the class creates.</summary>
    private static readonly ClassesSpec Small = new(
        [
            new ClassType("contract", "IThing", "interface"), new ClassType("thing", "Thing", "static"),
            new ClassType("made", "Made", "record")
        ],
        [
            new ClassRelation("thing", "contract", RelationKind.Implements),
            new ClassRelation("thing", "made", RelationKind.Creates)
        ]);

    /// <summary>The same with a plain class in the framework's tone that holds the thing and uses the record.</summary>
    private static readonly ClassesSpec Full = Small with
    {
        Types = [.. Small.Types, new ClassType("host", "Host", Tone: DiagramTone.Violet)],
        Relations =
        [
            .. Small.Relations, new ClassRelation("host", "thing", RelationKind.Holds),
            new ClassRelation("host", "made", RelationKind.Uses),
            new ClassRelation("thing", "host", RelationKind.Inherits)
        ]
    };

    private static List<(ClassKeyKind Kind, string Words)> Items(ClassesSpec spec, double width = 1600) =>
        [.. ClassKeyLayout.Of(spec, width).Items.Select(item => (item.Kind, item.Words))];

    /// <summary>
    /// The key has an item for each tone a badge wears, the tone a type's kind gives it when it names none, then
    /// each letter and each kind of link the picture has, and none for what it doesn't have.
    /// </summary>
    [Test]
    public void TheKey_HasAnItemOnlyForWhatThePictureHas() => Assert.That(Items(Small), Is.EqualTo(new[]
    {
        (ClassKeyKind.Tone, "this project"), (ClassKeyKind.Tone, "contracts and state"),
        (ClassKeyKind.Mark, "an interface"), (ClassKeyKind.Mark, "a record"), (ClassKeyKind.Mark, "static"),
        (ClassKeyKind.Link, "implements"), (ClassKeyKind.Link, "creates")
    }));

    /// <summary>
    /// The links stand with the structure first: holds, is a, implements, then creates and uses. A plain class's
    /// letter is explained too, and a named tone stands among the ones the kinds gave.
    /// </summary>
    [Test]
    public void TheLinks_StandWithTheStructureFirst()
    {
        var items = ClassKeyLayout.Of(Full, 1600).Items;
        var links = items.Where(item => item.Kind == ClassKeyKind.Link).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(links.Select(item => item.Link), Is.EqualTo(new RelationKind?[]
            {
                RelationKind.Holds, RelationKind.Inherits, RelationKind.Implements, RelationKind.Creates,
                RelationKind.Uses
            }));
            Assert.That(links.Select(item => item.Words),
                Is.EqualTo(new[] { "holds", "is a", "implements", "creates", "uses" }));
            Assert.That(items.Where(item => item.Kind == ClassKeyKind.Mark).Select(item => item.Mark), Is.EqualTo(
                new TypeMark?[] { TypeMark.Interface, TypeMark.Record, TypeMark.Static, TypeMark.Class }));
            Assert.That(items.Where(item => item.Kind == ClassKeyKind.Tone).Select(item => item.Tone),
                Is.EqualTo(new[] { DiagramTone.Green, DiagramTone.Violet, DiagramTone.Teal }));
            Assert.That(items.Select(item => item.Sample), Is.All.GreaterThan(0));
        });
    }

    /// <summary>
    /// The items stand side by side under the heading, each after the one before with room for its words, and
    /// the key is as tall as its heading and one row.
    /// </summary>
    [Test]
    public void TheItems_StandSideBySide_UnderTheHeading()
    {
        var (items, height) = ClassKeyLayout.Of(Small, 1600);

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Y).Distinct(), Is.EqualTo(new[] { ClassKeyLayout.Heading }));
            Assert.That(items[0].X, Is.Zero);
            Assert.That(items.Zip(items.Skip(1), (left, right) => right.X - left.X - left.Sample),
                Is.All.GreaterThan(ClassKeyLayout.Words));
            Assert.That(height, Is.EqualTo(ClassKeyLayout.Heading + ClassKeyLayout.Row));
        });
    }

    /// <summary>In a narrow picture an item that doesn't fit its row starts the next, at the left.</summary>
    [Test]
    public void InANarrowPicture_TheItemsWrap()
    {
        var (items, height) = ClassKeyLayout.Of(Full, 420);
        var rows = items.GroupBy(item => item.Y).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.GreaterThan(1));
            Assert.That(rows.Select(row => row.First().X), Is.All.Zero);
            Assert.That(height, Is.EqualTo(ClassKeyLayout.Heading + rows.Count * ClassKeyLayout.Row));
        });
    }
}
