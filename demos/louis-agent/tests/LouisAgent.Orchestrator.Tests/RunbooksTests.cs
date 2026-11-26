namespace LouisAgent.Orchestrator.Tests;

/// <summary>
/// Every method's runbook sets its policy in one Decisions table that the orchestrator and the triage manager both
/// read (FX257): what the investigation finds, the decision, and who. No runbook keeps the two sections it replaced.
/// </summary>
public class RunbooksTests
{
    /// <summary>The table's header, as every runbook writes it.</summary>
    private const string Header = "| The investigation finds | Decision | Who |";

    /// <summary>The decisions a row may name.</summary>
    private static readonly string[] Decisions = ["auto-fix", "hand-off", "person"];

    /// <summary>Each method's runbook as the build copies it, the service's own sheet aside.</summary>
    private static IEnumerable<string> Runbooks() =>
        Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "Skills", "order-service"), "*.md")
            .Where(file => Path.GetFileName(file) != "service.md");

    /// <summary>A runbook holds the table, with at least one row, and neither section it replaced.</summary>
    [TestCaseSource(nameof(Runbooks))]
    public void ARunbook_SetsItsPolicyInOneDecisionsTable(string runbook)
    {
        string text = File.ReadAllText(runbook);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("**Decisions:**").And.Contain(Header));
            Assert.That(Rows(text), Is.Not.Empty);
            Assert.That(text, Does.Not.Contain("**Escalate to:**").And.Not.Contain("**Auto-fixable:**"));
        });
    }

    /// <summary>Every row names one of the decisions the two readers know.</summary>
    [TestCaseSource(nameof(Runbooks))]
    public void EveryRow_NamesADecisionTheReadersKnow(string runbook) =>
        Assert.That(Rows(File.ReadAllText(runbook)).Select(row => row[1]),
            Has.All.Matches<string>(decision => Decisions.Contains(decision)));

    /// <summary>The table's rows after its header and rule, each as its three cells.</summary>
    private static List<string[]> Rows(string text) =>
    [
        .. text.Split('\n').SkipWhile(line => line.Trim() != Header).Skip(2)
            .TakeWhile(line => line.TrimStart().StartsWith('|'))
            .Select(line => line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray())
    ];
}
