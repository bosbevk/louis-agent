using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Agent.Toolsets;
using System.Collections.Concurrent;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Clock;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Sessions;

/// <summary>
/// One conversation: its engine (the toolsets it chose, fixed for its lifetime, F5), its history and the turn in
/// progress, if any.
/// </summary>
internal sealed class AgentSession(string id, AgentEngine engine, IReadOnlyList<string> toolsets, int depth, UsageTags tags,
    string? role = null, UserPlace? place = null, string? llm = null, string? tool = null, StreamFormat format = StreamFormat.Claude,
    SessionReplay? replay = null)
{
    private readonly SemaphoreSlim _turnLock = new(1, 1);
    private CancellationTokenSource? _activeTurn;

    public string Id { get; } = id;

    /// <summary>The format this session's events are written in, chosen when it opened and kept for its lifetime (F18 step 1f).</summary>
    public StreamFormat StreamFormat { get; } = format;

    /// <summary>The engine of this session's toolset profile, shared with every session that chose the same toolsets.</summary>
    public AgentEngine Engine { get; } = engine;

    /// <summary>The toolsets the session loaded, normalised (sorted, lowercase).</summary>
    public IReadOnlyList<string> Toolsets { get; } = toolsets;

    /// <summary>0 for a session a client opened; 1 for a sub-session another agent opened (F5-S5).</summary>
    public int Depth { get; } = depth;

    /// <summary>The role the session chose (F21), or null for the default voice (CL9).</summary>
    public string? Role { get; } = role;

    /// <summary>Where the user is, when they shared it (F22): set for every turn; the first message says it.</summary>
    public UserPlace? Place { get; } = place;

    /// <summary>What the session does with the web (F18 step 4a), resolved when it opened; null for a live chat and for an eval that leaves the web live.</summary>
    public SessionReplay? Replay { get; } = replay;

    /// <summary>The LLM profile the session runs on (F30), or null for the instance's own model.</summary>
    public string? Llm { get; } = llm;

    /// <summary>A minion's one tool (F30, depth 2); null for every other session, which has toolsets.</summary>
    public string? Tool { get; } = tool;

    /// <summary>The longest attachment kept, as in the prompt: one file can't fill a specialist's context.</summary>
    public const int MaxAttachmentChars = 100_000;

    private readonly ConcurrentDictionary<string, string> _attachments = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The files attached in this conversation, by name, for a specialist (F22, <see cref="Core.Tools.Agents.SessionAttachments"/>).</summary>
    public IReadOnlyDictionary<string, string> Attachments => _attachments;

    /// <summary>
    /// The specialists this chat keeps open for a follow-up (F26): current for each turn, so <c>StartSubSession</c> keeps
    /// them and <c>ContinueSubSession</c> finds them; closed on the peer when the chat is removed or dropped as idle.
    /// </summary>
    public SubSessionRegistry SubSessions { get; } = new();

    /// <summary>Keeps an attachment for later turns; the same name again replaces it.</summary>
    public void Remember(string name, string? content)
    {
        string text = content ?? string.Empty;
        _attachments[name] = text.Length <= MaxAttachmentChars ? text : text[..MaxAttachmentChars];
    }

    public List<ChatMessage> History { get; } = engine.NewHistory();

    /// <summary>What the caller said the session is for (e.g. the orchestrator's fix and run); on every usage record.</summary>
    public UsageTags Tags { get; } = tags;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivity { get; private set; } = DateTimeOffset.UtcNow;
    public bool IsBusy => _turnLock.CurrentCount == 0;

    /// <summary>Messages started in this session; the current one's number is the usage ledger's turn.</summary>
    public int Turns { get; private set; }

    /// <summary>The usage of the session's finished turns, for /usage and GET /sessions/{id} (F3), in the display currency (F14).</summary>
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
        try { turn.Cancel(); }
        catch (ObjectDisposedException) { return false; }
        return true;
    }
}
