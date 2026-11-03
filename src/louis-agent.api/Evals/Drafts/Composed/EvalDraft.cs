namespace LouisAgent.Api.Evals.Drafts.Composed;

/// <summary>
/// An eval being composed in the studio (F49.5), waiting to be checked, tried and published: the identity it proves
/// and its name, both read from its file; the file's text exactly as the person wrote it; its rubrics; who saved it
/// last and when. Like the other drafts it never changes the repository by itself: the publish writes the folder.
/// </summary>
/// <remarks>
/// The file stays text, not a parsed record: the runner is the one reader of eval files (finding R), so the API keeps
/// what was typed, layout and all, and only reads the two names it files the draft under.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
public sealed record EvalDraft(
    string Id,
    string Identity,
    string Name,
    string Json,
    IReadOnlyList<RubricFile> Rubrics,
    string Author,
    DateTimeOffset At);

/// <summary>One rubric of an eval being composed: its file's name in <c>Rubrics/</c> and its text.</summary>
public sealed record RubricFile(string Name, string Text);
