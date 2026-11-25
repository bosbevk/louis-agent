using LouisAgent.Core.Agent;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;

namespace LouisAgent.Acp.Turns;

/// <summary>
/// One Rider chat on this server: its working directory, the conversation, the turn running, and what it has used.
/// </summary>
internal sealed class AcpSession(string cwd, Conversation conversation, DisplayCurrency? display)
{
    /// <summary>The folder Rider opened the chat in.</summary>
    public string Cwd { get; } = cwd;

    /// <summary>The chat itself: everything said in it, the system prompt first.</summary>
    public Conversation Conversation { get; } = conversation;

    /// <summary>What a turn starting and a cancel lock on, so they never cross.</summary>
    public object SyncRoot { get; } = new();

    /// <summary>The turn running now, which a cancel stops; none between turns.</summary>
    public CancellationTokenSource? ActiveTurn { get; set; }

    /// <summary>Where the tools work: the repository of the file open in Rider, once a prompt names one.</summary>
    public string WorkingDirectory { get; set; } = cwd;

    /// <summary>Prompts answered by the model so far; the usage ledger's turn number.</summary>
    public int Turns;

    /// <summary>
    /// The usage of the session's finished turns, for the usage line's session total and /usage (F3), in the display
    /// currency (F14).
    /// </summary>
    public UsageSession Usage { get; } = new(display);
}
