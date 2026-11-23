namespace LouisAgent.Evals.Contracts.Runs;

/// <summary>
/// One play of a conversation, in a session of its own: its turns, each graded. It passed when every mark of every
/// turn is met, and a page asks it that, never working it out where it is shown.
/// </summary>
/// <param name="Number">Which repeat this is, from 1.</param>
/// <param name="Turns">The conversation's turns, in order, each with its marks.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed record RepeatRun(int Number, IReadOnlyList<TurnRun> Turns)
{
    /// <summary>Every fact and every criterion of every turn was met.</summary>
    public bool Passed => Turns.All(turn => turn.Facts.All(mark => mark.Met) && turn.Criteria.All(mark => mark.Met));
}
