namespace LouisAgent.Packs.Triage.Feed;

/// <summary>
/// Where a department's errors come from: what is new since the last look, one error by its id, and the mark that an
/// error has been handled so it is never handed out twice.
/// </summary>
/// <remarks>
/// Strategy (Gang of Four): the workflow reads errors the same way whatever reports them, a JSON-lines log today and
/// a tracker's webhook later (F100), so a department swaps its feed in configuration and the workflow never changes.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
public interface IErrorFeed
{
    /// <summary>The errors not handled yet, in the order they were reported.</summary>
    IReadOnlyList<ServiceError> ReadNew();

    /// <summary>An error this feed has read, by its id, so a tool can't be handed one the model invented.</summary>
    ServiceError? Find(string id);

    /// <summary>Remembers that an error was handled, across restarts.</summary>
    void MarkHandled(string id);
}
