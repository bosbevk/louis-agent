using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Runs;

namespace LouisAgent.Puls.Tests.Admin.Runs;

/// <summary>
/// A mode is named in the legend by who did its work: Puls with the roles it handed work to, or Puls alone.
/// </summary>
public class ModeLabelsTests
{
    /// <summary>One turn of a mode, answered with this role, or none.</summary>
    private static TurnRow Turn(string mode, string? role) =>
        new(new TurnToolsRow(0, 0, []), mode, 1, "task", 1, "s", "q", "a", role, null, 0, 0, [], 1, null);

    /// <summary>A mode whose turns handed work on names each role once; a mode that never did is Puls alone.</summary>
    [Test]
    public void AModeIsNamedByTheRolesItsTurnsUsed()
    {
        var labels = ModeLabels.Of([
            Turn("with-cook", "cook"), Turn("with-cook", "cook"), Turn("with-cook", "shopper"), Turn("alone", null),
            Turn("alone", "")
        ]);

        Assert.That(labels, Is.EqualTo(new Dictionary<string, string>
        {
            ["with-cook"] = "Puls with cook and shopper",
            ["alone"] = "Puls alone"
        }));
    }
}
