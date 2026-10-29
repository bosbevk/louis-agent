using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Web.Shared.Usage;

namespace LouisAgent.Puls.Admin.Explorer.Tree;

/// <summary>
/// One conversation as the explorer holds it: its task, who handled it, its errors, its grades and outcome, and its
/// turns.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed class ConversationNode
{
    /// <summary>Which task it is.</summary>
    public string Task { get; init; } = "";

    /// <summary>The specialist that handled it, if one did.</summary>
    public string? Role { get; init; }

    /// <summary>How many errors it had.</summary>
    public int Errors { get; init; }

    /// <summary>Its grades and outcome, shown the same for every eval.</summary>
    public ConversationGrades Grades { get; init; } = ConversationGrades.None;

    /// <summary>
    /// Its required graders all passed: the conversation's own outcome, the one the rings and the value count (R2-4).
    /// </summary>
    public bool Passed => Grades.Outcome == GradeOutcome.Pass;

    /// <summary>Its turns, in the order they happened once the tree is built.</summary>
    public List<TurnNode> Turns { get; } = [];

    /// <summary>All its turns' requests.</summary>
    public List<RequestNode> Rows => Turns.SelectMany(t => t.Rows).ToList();

    /// <summary>What its requests cost; nothing if none had a known price, never zero.</summary>
    public decimal? Cost => UsageFormat.KnownSum(Rows.Select(r => r.Cost));

    /// <summary>The conversation as the stored run sent it, with no turns yet.</summary>
    /// <remarks>Fowler's <i>Replace Constructor with Factory Function</i>, the guide's <c>Record.Of</c>.</remarks>
    public static ConversationNode Of(ConversationRow row) => new()
    {
        Task = row.Task,
        Role = row.Role,
        Errors = row.Errors,
        Grades = ConversationGrades.Of(row)
    };
}
