using LouisAgent.Web.Components.Editing;

namespace LouisAgent.Web.Tests.Components.Editing;

/// <summary>
/// F142: what a person does to a list they edit, as plain operations: add to its end, take one out, put one in
/// another's place, move one a step. Each hands back a new list and leaves the one it was given alone, since the
/// list belongs to whoever drew it.
/// </summary>
public class ListEditsTests
{
    private static readonly IReadOnlyList<string> Letters = ["a", "b", "c"];

    /// <summary>An item is added at the end.</summary>
    [Test]
    public void Added_PutsTheItemLast() =>
        Assert.That(Letters.Added("d"), Is.EqualTo(new[] { "a", "b", "c", "d" }));

    /// <summary>An item is taken out by its place.</summary>
    [Test]
    public void Without_TakesTheItemOut() =>
        Assert.That(Letters.Without(1), Is.EqualTo(new[] { "a", "c" }));

    /// <summary>An item is changed in its place.</summary>
    [Test]
    public void Replaced_ChangesTheItemInItsPlace() =>
        Assert.That(Letters.Replaced(1, "B"), Is.EqualTo(new[] { "a", "B", "c" }));

    /// <summary>An item moves a step earlier or later, changing places with its neighbour.</summary>
    [TestCase(1, -1, "b a c")]
    [TestCase(1, 1, "a c b")]
    public void Moved_ChangesPlacesWithTheNeighbour(int at, int step, string expected) =>
        Assert.That(string.Join(' ', Letters.Moved(at, step)), Is.EqualTo(expected));

    /// <summary>The first can't move earlier and the last can't move later: the list comes back as it was.</summary>
    [TestCase(0, -1)]
    [TestCase(2, 1)]
    public void Moved_PastAnEnd_ChangesNothing(int at, int step) =>
        Assert.That(Letters.Moved(at, step), Is.EqualTo(Letters));

    /// <summary>The list it was given is never changed.</summary>
    [Test]
    public void TheGivenList_IsLeftAlone()
    {
        _ = Letters.Added("d").Without(0).Moved(0, 1);

        Assert.That(Letters, Is.EqualTo(new[] { "a", "b", "c" }));
    }
}
