using System.Globalization;
using LouisAgent.Choices;

namespace LouisAgent.Puls.Admin.Explorer.Place;

/// <summary>
/// Where you are in the explorer: the view, and what is open in it (the conversation's task, the turn's number, a
/// request's time and mode, whether the list shows only the conversations to review). It is written into the run's
/// address and read back from it, so Back goes up a level and a link opens the same view.
/// </summary>
/// <remarks>
/// Memento, kept in the address (the URL as state): the explorer hands its place out and takes one back, and the
/// browser's history does the undo. A request is named by its time, since that is what the explorer keys it on
/// (<c>LevelKey</c>), in UTC round-trip form; a time that isn't one is no request, so a broken link opens the turn.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record ExplorerPlace(
    ExplorerView View,
    string? Task = null,
    int? Turn = null,
    DateTimeOffset? RequestAt = null,
    string? Mode = null,
    bool Review = false)
{
    /// <summary>The run itself, where the explorer starts.</summary>
    public static readonly ExplorerPlace Start = new(ExplorerView.Run);

    /// <summary>The depth of the view, as the explorer counts its levels.</summary>
    public int Level => (int)View;

    /// <summary>One conversation, open at its turns.</summary>
    public static ExplorerPlace Conversation(string task) => new(ExplorerView.Conversation, task);

    /// <summary>The place the address names, read once; the run when it names no view or one we don't have.</summary>
    public static ExplorerPlace Of(string? view, string? task, int? turn, string? request, string? mode, bool review)
    {
        var known = SettingChoice.TryParse(view, out ExplorerView? v) ? v ?? ExplorerView.Run : ExplorerView.Run;
        // A request's time comes back to the tick, or not at all.
        DateTimeOffset? at = DateTimeOffset.TryParseExact(request, "O", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
        return new ExplorerPlace(known, task, turn, at, mode, review);
    }

    /// <summary>The place as the address carries it: only what its view needs, each value escaped.</summary>
    public string Query()
    {
        var parts = new List<string> { $"view={View.Wire()}" };
        if (Task is { } task) parts.Add($"task={Uri.EscapeDataString(task)}");
        if (Turn is { } turn) parts.Add($"turn={turn.ToString(CultureInfo.InvariantCulture)}");
        if (RequestAt is { } at) parts.Add($"request={Uri.EscapeDataString(at.ToUniversalTime().ToString("O"))}");
        if (Mode is { } mode) parts.Add($"mode={Uri.EscapeDataString(mode)}");
        if (Review) parts.Add("review=true");
        return string.Join("&", parts);
    }
}
