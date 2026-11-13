using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using System.Collections.Concurrent;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.SessionStream.Wire.Requests;
using LouisAgent.SessionStream.Wire.Requests.Windows;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Sessions;

/// <summary>
/// One conversation: its engine (the toolsets it chose, fixed for its lifetime, F5), its history and the turn in
/// progress, if any.
/// </summary>
internal sealed class AgentSession(
    string id,
    AgentEngine engine,
    IReadOnlyList<string> toolsets,
    int depth,
    UsageTags tags,
    string? role = null,
    UserPlace? place = null,
    string? llm = null,
    string? tool = null,
    StreamFormat format = StreamFormat.Claude,
    SessionReplay? replay = null,
    SessionOpener? opener = null,
    TurnIdentity? who = null)
{
    /// <summary>
    /// Who this session is (F109.3): the identity it named, or the one its role belongs to, and whether it's a
    /// demonstration (F127.2); each turn runs as it.
    /// </summary>
    public TurnIdentity? Who { get; } = who;

    /// <summary>Who this session is, by name.</summary>
    public string? Identity => Who?.Name;

    private readonly SemaphoreSlim _turnLock = new(1, 1);
    private CancellationTokenSource? _activeTurn;

    public string Id { get; } = id;

    /// <summary>The signed-in person who opened it (F61.2), or null for a session a key opened.</summary>
    public SessionOpener? Opener { get; } = opener;

    /// <summary>
    /// The format this session's events are written in, chosen when it opened and kept for its lifetime (F18 step 1f).
    /// </summary>
    public StreamFormat StreamFormat { get; } = format;

    /// <summary>
    /// The engine of this session's toolset profile, shared with every session that chose the same toolsets.
    /// </summary>
    public AgentEngine Engine { get; } = engine;

    /// <summary>The toolsets the session loaded, normalised (sorted, lowercase).</summary>
    public IReadOnlyList<string> Toolsets { get; } = toolsets;

    /// <summary>0 for a session a client opened; one more for each agent asked below it (F5-S5, F127.5).</summary>
    public int Depth { get; } = depth;

    /// <summary>
    /// The agents this session was asked through, the first one first, and its own last (F127.5): an ask from it is
    /// refused for an agent already here.
    /// </summary>
    public IReadOnlyList<string> Chain { get; init; } = [];

    /// <summary>
    /// The pages of the agents' windows on the visitor's screen, from their last message (FX134): an ask to one of
    /// those agents sends it its page.
    /// </summary>
    public IReadOnlyList<WindowPageRequest> Windows { get; set; } = [];

    /// <summary>
    /// The page the latest message came from (F127.8): its address and part of the site go in the turn log.
    /// </summary>
    public PageRequest? Page { get; set; }

    /// <summary>The role the session chose (F21), or null for the default voice (CL9).</summary>
    public string? Role { get; } = role;

    /// <summary>Where the user is, when they shared it (F22): set for every turn; the first message says it.</summary>
    public UserPlace? Place { get; } = place;

    /// <summary>
    /// What the session does with the web (F18 step 4a), resolved when it opened; null for a live chat and for an eval
    /// that leaves the web live.
    /// </summary>
    public SessionReplay? Replay { get; } = replay;

    /// <summary>The LLM profile the session runs on (F30), or null for the instance's own model.</summary>
    public string? Llm { get; } = llm;

    /// <summary>A minion's one tool (F30, depth 2); null for every other session, which has toolsets.</summary>
    public string? Tool { get; } = tool;

    /// <summary>The longest attachment kept, as in the prompt: one file can't fill an asked agent's context.</summary>
    public const int MaxAttachmentChars = 100_000;

    private readonly ConcurrentDictionary<string, string> _attachments = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The files attached in this conversation, by name, for an agent it asks (F22, <see cref="SessionAttachments"/>).
    /// </summary>
    public IReadOnlyDictionary<string, string> Attachments => _attachments;

    /// <summary>
    /// The agents this chat keeps open for a follow-up (F26): current for each turn, so <c>AskAgent</c> keeps them and
    /// finds them again by address; closed on the peer when the chat is removed or dropped as idle.
    /// </summary>
    public SubSessionRegistry SubSessions { get; } = new();

    /// <summary>Keeps an attachment for later turns; the same name again replaces it.</summary>
    public void Remember(string name, string? content)
    {
        string text = content ?? string.Empty;
        _attachments[name] = text.Length <= MaxAttachmentChars ? text : text[..MaxAttachmentChars];
    }

    public List<ChatMessage> History { get; } = engine.NewHistory();

    /// <summary>
    /// What the caller said the session is for (e.g. the orchestrator's fix and run); on every usage record.
    /// </summary>
    public UsageTags Tags { get; } = tags;

    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivity { get; private set; } = DateTimeOffset.UtcNow;
    public bool IsBusy => _turnLock.CurrentCount == 0;

    /// <summary>Messages started in this session; the current one's number is the usage ledger's turn.</summary>
    public int Turns { get; private set; }

    /// <summary>
    /// The usage of the session's finished turns, for /usage and GET /v1/sessions/{id} (F3), in the display currency
    /// (F14).
    /// </summary>
    public UsageSession Usage { get; } = new(engine.Display);

    /// <summary>Starts a turn unless one is already running; the turn stops if the client disconnects.</summary>
    public bool TryStartTurn(CancellationToken requestAborted, out CancellationTokenSource turn)
    {
        turn = null!;
        if (!_turnLock.Wait(0)) return false;

        turn = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        _activeTurn = turn;
        Turns++;
        LastActivity = DateTimeOffset.UtcNow;
        return true;
    }

    public void EndTurn(CancellationTokenSource turn)
    {
        Interlocked.CompareExchange(ref _activeTurn, null, turn);
        turn.Dispose();
        LastActivity = DateTimeOffset.UtcNow;
        _turnLock.Release();
    }

    /// <summary>Stops the turn in progress; returns false when there is none.</summary>
    public bool Cancel()
    {
        if (Volatile.Read(ref _activeTurn) is not { } turn) return false;
        try
        {
            turn.Cancel();
        }
        catch (ObjectDisposedException)
        {
            return false;
        }

        return true;
    }
}
