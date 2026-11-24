using System.Text.Json;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Files.Pages;
using LouisAgent.Evals.Files.Json;
using System.ComponentModel;

namespace LouisAgent.Evals.Files;

/// <summary>
/// One conversation a person has with the agent: its id, the turns they take, and the page they are on, named by its
/// address or given in the file.
/// </summary>
/// <param name="Id">The conversation's name, used once in its file.</param>
/// <param name="Turns">The turns the person takes, in order.</param>
/// <param name="Page">
/// The page a site's guide is on, an address starting with <c>/</c>; none for an agent off a site.
/// </param>
public sealed record Conversation(
    [property: Description("The conversation's name, used once in the file.")]
    string Id,
    [property: Description("The turns the person takes, in order.")]
    IReadOnlyList<Turn> Turns,
    [property: Description("The page the person is on when the agent is a site's guide, starting with /.")]
    string? Page = null)
{
    /// <summary>
    /// The page given in the file itself, as content, instead of an address (F126.5.1); a conversation names one or
    /// the other, never both.
    /// </summary>
    [Description("A page given as content instead of an address, never both: its url, title and text.")]
    public GivenPage? GivenPage { get; init; }

    /// <summary>
    /// A run's conversation made back into one of the eval file, to copy into it and change (F126.4): each turn's ask,
    /// its facts' words and its criteria's words, from its first repeat, on the page the file gives it.
    /// </summary>
    /// <param name="run">The run's conversation.</param>
    /// <param name="page">The page the eval file gives the conversation, or none.</param>
    public static Conversation Of(ConversationRun run, string? page)
    {
        var turns = run.Repeats.FirstOrDefault()?.Turns.Select(turn => new Turn(turn.Transcript.Ask)
        {
            // A fact's mark keeps the fact's own words, so they read back as the fact.
            Facts = [.. turn.Facts.Select(mark => Fact.Parse(mark.Text))],
            Criteria = [.. turn.Criteria.Select(mark => mark.Text)]
        });
        return new Conversation(run.Id, [.. turns ?? []], page);
    }

    /// <summary>
    /// The conversation as the eval file writes it, with the file's own options, so the shape is said once and the
    /// file's reader reads it back (ADR 0134).
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this, EvalJson.Options);
}
