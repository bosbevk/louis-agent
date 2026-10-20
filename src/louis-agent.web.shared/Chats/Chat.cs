namespace LouisAgent.Web.Shared.Chats;

using LouisAgent.Web.Shared.Usage;

/// <summary>A chat as the browser keeps it; the server only holds the session (and loses it on restart).</summary>
public sealed class Chat
{
    public required string SessionId { get; init; }
    public string Title { get; set; } = "New chat";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    public List<Turn> Turns { get; init; } = [];

    /// <summary>The API no longer knows this session (it was restarted), so the chat can be read but not continued.</summary>
    public bool Ended { get; set; }

    /// <summary>Pinned under Gunstelinge in Puls's sidebar (F20).</summary>
    public bool Favourite { get; set; }

    /// <summary>Moved out of the recent chats into Geargiveer in Puls's sidebar (F20); still opens and continues.</summary>
    public bool Archived { get; set; }

    /// <summary>
    /// The chat's usage: the sum of its turns' (F3). Added up here, not asked from the API, because the browser keeps the
    /// chat after the API's session is gone.
    /// </summary>
    public TurnUsage? Usage() =>
        Turns.Select(t => t.Usage).OfType<TurnUsage>().Aggregate((TurnUsage?)null, (total, usage) => total is null ? usage : total + usage);
}
