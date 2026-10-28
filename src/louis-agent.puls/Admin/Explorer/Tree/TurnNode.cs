using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Usage;

namespace LouisAgent.Puls.Admin.Explorer.Tree;

/// <summary>
/// One turn as the explorer holds it: what was asked and answered, who answered, how long it took, the sites it read,
/// what its tools did, and the model requests it made.
/// </summary>
/// <remarks>
/// A class, not a record: the tree is built in passes, and the requests are hung on their turn after it is made.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed class TurnNode
{
    /// <summary>The turn's number in its conversation.</summary>
    public int N { get; init; }

    /// <summary>What the person asked.</summary>
    public string Question { get; init; } = "";

    /// <summary>What Puls answered, if it did.</summary>
    public string? Answer { get; init; }

    /// <summary>The specialist that answered, if one did.</summary>
    public string? Role { get; init; }

    /// <summary>The specialist's session this turn carried on, if it did.</summary>
    public string? Continued { get; init; }

    /// <summary>Why the turn failed, if it did.</summary>
    public string? Error { get; init; }

    /// <summary>How long the turn took, wall time.</summary>
    public double Seconds { get; init; }

    /// <summary>The sites it read pages from.</summary>
    public IReadOnlyList<string> Hosts { get; init; } = [];

    /// <summary>How many tool calls it made.</summary>
    public int ToolCalls { get; init; }

    /// <summary>How many of its tool calls were refused or failed.</summary>
    public int ToolsFailed { get; init; }

    /// <summary>Why its tools failed, per tool, as the API counted them from the stored events.</summary>
    public IReadOnlyList<ToolReasonRow> ToolReasons { get; init; } = [];

    /// <summary>The model requests it made, from the ledger.</summary>
    public List<RequestNode> Rows { get; } = [];

    /// <summary>What its requests cost; nothing if none had a known price, never zero.</summary>
    public decimal? Cost => UsageFormat.KnownSum(Rows.Select(r => r.Cost));

    /// <summary>
    /// The transcript before any replay: the question, and the answer as one block of text. Made once, so every draw
    /// hands the turn's view the same transcript.
    /// </summary>
    public Turn Shown => _shown ??= Transcript();

    private Turn? _shown;

    /// <summary>Writes the transcript out of the question, the answer and the error.</summary>
    private Turn Transcript()
    {
        var shown = new Turn
        {
            UserText = Question,
            Error = Error
        };
        if (Answer is { Length: > 0 } answer)
            shown.Blocks.Add(new Block
            {
                Kind = BlockKind.Text,
                Text = answer
            });
        return shown;
    }

    /// <summary>The turn as the stored run sent it, with no requests yet.</summary>
    /// <remarks>
    /// Fowler's <i>Replace Constructor with Factory Function</i>. A turn from before the tool counts has none, so
    /// it reads as no calls.
    /// </remarks>
    public static TurnNode Of(TurnRow row) => new()
    {
        N = row.Turn,
        Question = row.Question ?? "",
        Answer = row.Answer,
        Role = row.Role,
        Continued = row.Continued,
        Error = row.Error,
        Seconds = row.Seconds,
        Hosts = row.Hosts ?? [],
        ToolCalls = row.Tools?.Calls ?? 0,
        ToolsFailed = row.Tools?.Failed ?? 0,
        ToolReasons = row.Tools?.Reasons ?? []
    };
}
