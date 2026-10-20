using LouisAgent.Core.Tools.Documents;

namespace LouisAgent.Core.Tests.Tools.Documents;

// S (CL15): the table ReadTable shows: a header, the separator, one row per line; short rows padded to the width, a
// pipe in a cell escaped, a line break in a cell made a space.
public class MarkdownTableTests
{
    private static string Lines(params string[] lines) => string.Concat(lines.Select(l => l + Environment.NewLine));

    [Test]
    public void AHeaderASeparatorAndTheRows()
    {
        string table = MarkdownTable.Render(["Item", "Prys"], [["Rys", "24.99"], ["Melk", "18.50"]], columns: 2);

        Assert.That(table, Is.EqualTo(Lines("| Item | Prys |", "| --- | --- |", "| Rys | 24.99 |", "| Melk | 18.50 |")));
    }

    [Test]
    public void ShortRowsArePadded_PipesEscaped_LineBreaksFlattened()
    {
        string table = MarkdownTable.Render(["a"], [["x|y", "two\nlines"], []], columns: 3);

        Assert.That(table, Is.EqualTo(Lines("| a |  |  |", "| --- | --- | --- |", "| x\\|y | two lines |  |", "|  |  |  |")));
    }
}
