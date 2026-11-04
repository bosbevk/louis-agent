using System.Globalization;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.SessionStream;
using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Puls.Admin.LiveRuns;

/// <summary>
/// One turn of the run in progress: its question and its stream for the chat's turn view, and its figures once it
/// has ended.
/// </summary>
/// <remarks>
/// A class, not a record: the row is a thing that changes while the run goes on (its stream grows, its end comes),
/// and the table keeps the same row from its question to its end.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed class LiveTurn(TurnKey key)
{
    /// <summary>Which turn it is.</summary>
    public TurnKey Key { get; } = key;

    /// <summary>The turn's mode.</summary>
    public string Mode => Key.Mode;

    /// <summary>The turn's repeat.</summary>
    public int Repeat => Key.Repeat;

    /// <summary>The turn's conversation.</summary>
    public string Task => Key.Task;

    /// <summary>The turn's number in its conversation.</summary>
    public int N => Key.N;

    /// <summary>The question and the answer as the chat shows them.</summary>
    public Turn Turn { get; set; } = new();

    /// <summary>Its end came: the script's turn event, or the store's turn.</summary>
    public bool Done { get; set; }

    /// <summary>Its stream came on this page; otherwise it is replayed from the store when it is opened.</summary>
    public bool Streamed { get; set; }

    /// <summary>The specialist that answered, if one did.</summary>
    public string? Role { get; set; }

    /// <summary>The sub-session a follow-up went on with, if it did.</summary>
    public string? Continued { get; set; }

    /// <summary>Why the turn failed, if it did.</summary>
    public string? Error { get; set; }

    /// <summary>How many tools the mode's agent (Puls) called itself.</summary>
    public int AgentCalls { get; set; }

    /// <summary>How many tools the specialist called.</summary>
    public int SpecialistCalls { get; set; }

    /// <summary>How long the turn took.</summary>
    public double Seconds { get; set; }

    /// <summary>
    /// Who answered: the specialist, the sub-session it went on with, or Puls itself. Blank until it's done.
    /// </summary>
    /// <remarks>A switch on the row's shape, one arm per case, instead of nested ternaries.</remarks>
    public string RoleText => this switch
    {
        { Done: false } => "",
        { Role: { } role } => role,
        { Continued: { } sub } => $"continued · {sub}",
        _ => "Puls itself"
    };

    /// <summary>How long it took, with one decimal and a point on any machine. Blank until it's done.</summary>
    public string SecondsText => Done ? Seconds.ToString("0.0", CultureInfo.InvariantCulture) : "";

    /// <summary>Takes the figures the runner sent when the turn ended.</summary>
    public void Finish(TurnEvent ended) => Finish(ended.Role, ended.Continued, ended.Error, ended.AgentCalls,
        ended.SpecialistCalls, ended.Seconds);

    /// <summary>Takes the figures the store kept of the turn.</summary>
    public void Finish(TurnRow stored) => Finish(stored.Role, stored.Continued, stored.Error, stored.AgentCalls,
        stored.SpecialistCalls, stored.Seconds);

    /// <summary>
    /// Plays the store's stream events of a turn this page never streamed into a fresh turn with the same question,
    /// so it shows as if it had streamed here.
    /// </summary>
    public void Replay(IEnumerable<TurnEventRow> events)
    {
        // Start from the question alone, then let each stored event build the answer as the live stream would.
        var turn = new Turn { UserText = Turn.UserText };
        foreach (var e in events) TurnStream.Apply(turn, SessionEvent.From(e.Type, e.Data));
        Turn = turn;
        Streamed = true;
    }

    /// <summary>Marks the turn done with its figures.</summary>
    private void Finish(string? role, string? continued, string? error, int agentCalls, int specialistCalls,
        double seconds)
    {
        Done = true;
        Role = role;
        Continued = continued;
        Error = error;
        AgentCalls = agentCalls;
        SpecialistCalls = specialistCalls;
        Seconds = seconds;
    }
}
