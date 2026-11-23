namespace LouisAgent.Evals.Contracts.Grading;

/// <summary>
/// A person saying the judge got a criterion wrong: when they said so, and why, if they wrote it down. A flipped mark
/// counts the other way from what the judge said, and keeps the judge's own answer beside it.
/// </summary>
/// <remarks>
/// Kept on the mark it corrects (Louis's pick, 2026-11-23, "On the mark itself"), over an audit list of flips beside
/// the run and over an event-sourced file of flips replayed onto it (Fowler's Audit Log and Event Sourcing): the
/// mark is the Information Expert for which answer counts, so every sum asks it and nothing has to join two lists.
/// The note is one optional line (his pick, "An optional one-line note").
/// </remarks>
/// <param name="At">When it was flipped, in UTC.</param>
/// <param name="Note">Why, in one line, or none.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed record Flip(DateTimeOffset At, string? Note = null);
