namespace LouisAgent.AcpServer.Turns;

using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

/// <summary>One Rider chat on this server: its working directory, its history, the turn running, and what it has used.</summary>
internal sealed class AcpSession(string cwd, List<ChatMessage> history, DisplayCurrency? display)
{
    public string Cwd { get; } = cwd;
    public List<ChatMessage> History { get; } = history;
    public object SyncRoot { get; } = new();
    public CancellationTokenSource? ActiveTurn { get; set; }
    public string WorkingDirectory { get; set; } = cwd;

    /// <summary>Prompts answered by the model so far; the usage ledger's turn number.</summary>
    public int Turns;

    /// <summary>The usage of the session's finished turns, for the usage line's session total and /usage (F3), in the display currency (F14).</summary>
    public UsageSession Usage { get; } = new(display);
}
