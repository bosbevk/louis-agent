using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Charts;
using LouisAgent.Puls.Admin.Explorer.Legends;
using LouisAgent.Puls.Admin.Explorer.Tree;

namespace LouisAgent.Puls.Tests.Admin.Explorer.Legends;

/// <summary>The legends of the explorer's charts.</summary>
public class ExplorerLegendsTests
{
    /// <summary>Each mode in its palette colour, named for who it is.</summary>
    [Test]
    public void TheModes_AreInThePalettesColours()
    {
        var run = new StoredRunReply(
            new StoredRunRow("run1", "check", 1, "done", 0, DateTimeOffset.UnixEpoch, null, null, null, null, null,
                null,
                null, null, null), null,
            [
                new ModeRow(SampleMode.Alpha, 1, [], null, null, null, null, null, null, null),
                new ModeRow(SampleMode.Beta, 1, [], null, null, null, null, null, null, null)
            ], null, [], null, [], [], [], []);

        Assert.That(ExplorerLegends.Modes(RunTree.Of(run, 1)),
            Is.EqualTo(new[]
            {
                new LegendEntry(ChartPalette.Series(0), "Puls alone"),
                new LegendEntry(ChartPalette.Series(1), "Puls alone")
            }));
    }

    /// <summary>Puls's own part, then the specialists' under their roles' name.</summary>
    [Test]
    public void TheParts_ArePulsThenTheSpecialists() =>
        Assert.That(ExplorerLegends.Parts(SampleRole.Alpha),
            Is.EqualTo(new[]
            {
                new LegendEntry(ExplorerColours.Own, "Puls itself"),
                new LegendEntry(ExplorerColours.Spec, SampleRole.Alpha)
            }));
}
