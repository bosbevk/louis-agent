using LouisAgent.Web.Components.Screens.Forms;

namespace LouisAgent.Web.Tests.Components.Screens.Forms;

/// <summary>A list you edit a row per line: add, remove and move, and the order shown is the order kept.</summary>
public class ScreenRowsEditorTests
{
    /// <summary>The editor over these two rows, and the list it edits.</summary>
    private static (IRenderedComponent<ScreenRowsEditor> Editor, List<List<string>> Rows) Render(BunitContext ctx)
    {
        List<List<string>> rows = [["Bo", "Meng"], ["", "Bak"]];
        var editor = ctx.Render<ScreenRowsEditor>(p => p
            .Add(c => c.Columns, ["Section", "Text"])
            .Add(c => c.Rows, rows));
        return (editor, rows);
    }

    /// <summary>A row per line, a box per cell, with what it holds.</summary>
    [Test]
    public void EachRow_HasABoxPerCell()
    {
        using var ctx = new BunitContext();

        var (editor, _) = Render(ctx);

        Assert.That(editor.FindAll(".screen-row"), Has.Count.EqualTo(2));
        Assert.That(editor.FindAll(".screen-row")[0].QuerySelectorAll("input").Select(input => input.GetAttribute("value")),
            Is.EqualTo(new[] { "Bo", "Meng" }));
    }

    /// <summary>Typing changes that cell.</summary>
    [Test]
    public void Typing_ChangesTheCell()
    {
        using var ctx = new BunitContext();
        var (editor, rows) = Render(ctx);

        editor.FindAll(".screen-row input")[1].Change("Roer");

        Assert.That(rows[0][1], Is.EqualTo("Roer"));
    }

    /// <summary>Add puts an empty row at the end, with a cell per column.</summary>
    [Test]
    public void Add_PutsAnEmptyRowAtTheEnd()
    {
        using var ctx = new BunitContext();
        var (editor, rows) = Render(ctx);

        editor.Find(".screen-row-add").Click();

        Assert.That(rows, Has.Count.EqualTo(3));
        Assert.That(rows[2], Is.EqualTo(new[] { "", "" }));
    }

    /// <summary>Remove takes that row out.</summary>
    [Test]
    public void Remove_TakesThatRowOut()
    {
        using var ctx = new BunitContext();
        var (editor, rows) = Render(ctx);

        editor.FindAll(".screen-row-remove")[0].Click();

        Assert.That(rows.Single()[1], Is.EqualTo("Bak"));
    }

    /// <summary>Moving swaps it with its neighbour.</summary>
    [Test]
    public void Moving_SwapsItWithItsNeighbour()
    {
        using var ctx = new BunitContext();
        var (editor, rows) = Render(ctx);

        editor.FindAll(".screen-row-up")[1].Click();

        Assert.That(rows.Select(row => row[1]), Is.EqualTo(new[] { "Bak", "Meng" }));
    }

    /// <summary>The first can't go up and the last can't go down.</summary>
    [Test]
    public void TheEnds_CannotMovePastTheEnd()
    {
        using var ctx = new BunitContext();

        var (editor, _) = Render(ctx);

        Assert.That(editor.FindAll(".screen-row-up")[0].HasAttribute("disabled"), Is.True);
        Assert.That(editor.FindAll(".screen-row-down")[1].HasAttribute("disabled"), Is.True);
    }
}
