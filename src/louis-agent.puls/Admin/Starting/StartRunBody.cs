using System.Text.Json.Serialization;

namespace LouisAgent.Puls.Admin.Starting;

/// <summary>
/// What the page posts to start a run: how many repeats, and for a replay the recorded run to play back (FX28). The
/// check is in the address.
/// </summary>
/// <param name="Repeat">How many times each conversation runs.</param>
/// <param name="Replay">The recorded run to play back; left off the wire when there is none.</param>
/// <remarks>
/// A named body instead of an anonymous object, so the test can name what the API reads. The API keeps its own
/// <c>StartRunRequest</c> for now, with the eval's web settings this page doesn't send (the unattended run's
/// finding 9). <see cref="Of"/> is Fowler's <i>Replace Constructor with Factory Function</i>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record StartRunBody(
    int Repeat,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Replay = null)
{
    /// <summary>The body for a card's Run button or a row's Replay button.</summary>
    public static StartRunBody Of(StartRequest request) => new(request.Repeats, request.Replay);
}
