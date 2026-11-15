using LouisAgent.Web.Chats.Calls;

namespace LouisAgent.Web.Components.Widget.Bar;

/// <summary>
/// The chips over the agent's bar (F134.3): the questions a press asks, and the one drawn bright. Before anything is
/// asked they are the site's own. Once there is a talk they follow it: the site's first question stays, and the rest
/// are what the agent last offered, so a chip is always a next step from where the talk was.
/// </summary>
/// <remarks>
/// Ask the type: the bar asks this record which chips to draw and only draws them, so the rule lives in one place
/// with its own tests.
/// </remarks>
/// <param name="Questions">Each chip's words, which are the question it asks.</param>
/// <param name="Suggested">The chip drawn bright; none when there are no chips.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record AgentChips(IReadOnlyList<string> Questions, string? Suggested)
{
    /// <summary>How many of the agent's last choices become chips, after the site's first question.</summary>
    private const int FromTheTalk = 2;

    /// <summary>No chips.</summary>
    private static readonly AgentChips None = new([], null);

    /// <summary>
    /// The chips for a site's questions and what the agent last offered. With no talk yet they are the site's. After
    /// an answer that offered choices, they are the site's first question and then the agent's choices, its
    /// suggested one first and bright. A choice the visitor writes themselves is the box's, so it is no chip.
    /// </summary>
    public static AgentChips Of(AgentSuggestions? site, Offered? last)
    {
        if (site?.Suggested is not { } first) return None;
        if (last is null) return new AgentChips(site.Questions, first);

        var next = last.Choices.Where(choice => choice.Writes is null)
            .OrderByDescending(choice => choice.Suggested)
            .Select(choice => choice.Label)
            .Where(label => label != first)
            .Take(FromTheTalk)
            .ToList();
        string bright = last.Choices.FirstOrDefault(choice => choice is { Suggested: true, Writes: null })?.Label
                        ?? first;
        return new AgentChips([first, .. next], bright);
    }
}
