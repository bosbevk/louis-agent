using LouisAgent.Api.Evals.Store.Notes;
using LouisAgent.Api.Evals.Store.Runs;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;

namespace LouisAgent.Api.Evals.Store;

/// <summary>
/// Everything an eval run keeps, in one place: the record stores together, plus the notes and deleting a whole run,
/// which reach across them. In memory for the tests and an API with no database, in MongoDB for the real runs.
/// </summary>
/// <remarks>
/// Facade (Gang of Four) over one Repository per record (Fowler, PoEAA). The recorder, the endpoints and the reports
/// keep asking one store as they always did, while each record's store is its own small class with its own contract
/// test. A caller that needs only one record can take that record's interface instead, since this one is all of them
/// (Interface Segregation). Without the facade every handler and test would have to be handed four stores.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public interface IEvalStore : IRunStore
{
    /// <summary>Saves one conversation of a run, replacing the one under its key.</summary>
    Task SaveConversationAsync(string run, Conversation conversation, CancellationToken cancellationToken = default);

    /// <summary>A run's conversations, in the order they started.</summary>
    Task<IReadOnlyList<Conversation>> ConversationsAsync(string run, CancellationToken cancellationToken = default);

    /// <summary>Saves a turn together with its stream events, replacing the ones under its key.</summary>
    Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events,
        CancellationToken cancellationToken = default);

    /// <summary>A conversation's turns in order, without their stream events, which are heavy.</summary>
    Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// One turn's stream events in the order they came, for replaying it. Nothing for a turn we don't know.
    /// </summary>
    Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn,
        CancellationToken cancellationToken = default);

    /// <summary>Adds the model requests the ledger has for this run.</summary>
    Task AddRequestsAsync(string run, IReadOnlyList<Request> requests, CancellationToken cancellationToken = default);

    /// <summary>A run's model requests, by time.</summary>
    Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a person's note to a run, or to one of its conversations. Notes are only ever added, and saving the run
    /// again never touches them. You find out whether it was kept, the run isn't there, or the text was turned down.
    /// </summary>
    Task<NoteResult> AddNoteAsync(string run, string? text, DateTimeOffset at, ConversationKey? conversation = null,
        CancellationToken cancellationToken = default);

    /// <summary>A run's notes, from the run and its conversations, in the order they were written.</summary>
    Task<IReadOnlyList<RunNote>> NotesAsync(string run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a run and everything under it: conversations, turns, requests and notes. Tells you whether there was a
    /// run.
    /// </summary>
    Task<bool> DeleteRunAsync(string id, CancellationToken cancellationToken = default);
}
