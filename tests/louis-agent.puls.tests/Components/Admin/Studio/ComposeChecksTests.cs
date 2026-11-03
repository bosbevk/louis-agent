using System.Text.Json;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Puls.Admin.Studio.Compose;
using LouisAgent.Puls.Components.Admin.Studio.Compose;

namespace LouisAgent.Puls.Tests.Components.Admin.Studio;

/// <summary>
/// The compose page's checks (F49.5): a check is picked from the runner's catalogue, its fields drawn from its schema
/// (a required one marked), a reader picked from the readers the catalogue has, and what is typed goes into the eval.
/// </summary>
public class ComposeChecksTests
{
    /// <summary>The catalogue: one check that reads a total, and two readers.</summary>
    private static readonly CheckListReply Catalogue = new(
        [
            new CheckTypeRow("amount-under", "fixed-rule", JsonDocument.Parse(
                    """{"type":["object","null"],"properties":{"key":{"type":"string"},"reader":{"type":"string"}},"required":["key","reader"]}""")
                .RootElement.Clone()),
            new CheckTypeRow("no-error", "fixed-rule", JsonDocument.Parse("""{"type":["object","null"]}""").RootElement
                .Clone())
        ],
        [new ReaderRow("rand-total", "money"), new ReaderRow("digits", "number")]);

    /// <summary>Renders the checks of an eval that has one amount-under check.</summary>
    private static (IRenderedComponent<ComposeChecks> Cut, EvalComposition Eval) Render()
    {
        var ctx = new BunitContext();
        var eval = new EvalComposition();
        eval.Checks.Add(new CheckEntry { Type = "amount-under" });
        var cut = ctx.Render<ComposeChecks>(p => p.Add(x => x.Eval, eval).Add(x => x.Catalogue, Catalogue));
        return (cut, eval);
    }

    /// <summary>The check's fields come from its schema, the required ones marked.</summary>
    [Test]
    public void ACheck_ShowsTheFieldsOfItsSchema_TheRequiredOnesMarked()
    {
        var (cut, _) = Render();

        Assert.That(cut.FindAll(".compose-field-name").Select(f => f.TextContent.Trim()),
            Is.EqualTo(new[] { "key *", "reader *" }));
    }

    /// <summary>A reader is picked from the catalogue's readers.</summary>
    [Test]
    public void AReader_IsPickedFromTheCataloguesReaders() =>
        Assert.That(Render().Cut.FindAll(".compose-reader option").Select(o => o.TextContent.Trim()),
            Is.SupersetOf(new[] { "rand-total", "digits" }));

    /// <summary>What is typed goes into the eval, by the setting's name.</summary>
    [Test]
    public void WhatIsTyped_GoesIntoTheEval()
    {
        var (cut, eval) = Render();

        cut.Find(".compose-value").Change("budget");
        cut.Find(".compose-reader").Change("rand-total");

        Assert.That((eval.Checks[0].Values["key"], eval.Checks[0].Values["reader"]),
            Is.EqualTo(("budget", "rand-total")));
    }

    /// <summary>A check is added from the catalogue, and one is removed.</summary>
    [Test]
    public void AChecks_IsAdded_AndRemoved()
    {
        var (cut, eval) = Render();

        cut.Find(".compose-add-type").Change("no-error");
        cut.Find(".compose-add-check").Click();
        cut.FindAll(".compose-remove-check")[0].Click();

        Assert.That(eval.Checks.Select(c => c.Type), Is.EqualTo(new[] { "no-error" }));
    }
}
