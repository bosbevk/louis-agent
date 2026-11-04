namespace LouisAgent.Api.Evals.Store.Deletion;

/// <summary>
/// How much went when a run was deleted, collection by collection: its own record, its conversations, turns, requests
/// and notes, and what people said about it (their grades, the judge's checks). The eval store fills its five; the
/// grades and checks live in their own stores, which <see cref="RunDeletion"/> adds.
/// </summary>
/// <remarks>
/// Replace Primitive with Object (Fowler): deleting a run used to say only "there was something". The counts let the
/// start-up clean-up say what it removed, while the delete button still asks only <see cref="Any"/>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is compatible before a sale</seealso>
public sealed record DeletedRun(
    int Runs,
    int Conversations,
    int Turns,
    int Requests,
    int Notes,
    int Grades = 0,
    int Calibrations = 0)
{
    /// <summary>Whether anything was there to delete, even a leftover with no run record of its own.</summary>
    public bool Any => Runs + Conversations + Turns + Requests + Notes + Grades + Calibrations > 0;
}
