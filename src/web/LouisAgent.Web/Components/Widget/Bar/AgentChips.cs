using LouisAgent.Web.Chats.Calls;

namespace LouisAgent.Web.Components.Widget.Bar;

/// <summary>
/// The chips over the agent's bar (F134.3): the questions a press asks, and the one drawn bright. Before anything is
/// asked they are the site's own. Once there is a talk they follow it: what the agent last offered comes first, so a
/// chip is first a next step from where the talk was, and the site's own questions fill the places left (FX173,
/// Louis: "the agents own idea must take priority").
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
    /// <summary>How many of the agent's last choices become chips, first of them.</summary>
    private const int FromTheTalk = 3;

    /// <summary>How many chips there are at most: the places round the closed bar's orb that a chip takes (FX173).</summary>
    private const int Places = 5;

    /// <summary>No chips.</summary>
    private static readonly AgentChips None = new([], null);

    /// <summary>
    /// The chips for a site's questions and what the agent last offered. With no talk yet they are the site's. After
    /// an answer that offered choices, the agent's choices come first, its suggested one first and bright, and the
    /// site's questions fill the places left, none twice. A choice the visitor writes themselves is the box's, so it
    /// is no chip.
    /// </summary>
    public static AgentChips Of(AgentSuggestions? site, Offered? last)
    {
        if (site?.Suggested is not { } first) return None;
        if (last is null) return new AgentChips(site.Questions, first);

        var ideas = last.Choices.Where(choice => choice.Writes is null)
            .OrderByDescending(choice => choice.Suggested)
            .Select(choice => choice.Label)
            .Take(FromTheTalk)
            .ToList();
        var questions = ideas.Concat(site.Questions.Except(ideas)).Take(Places).ToList();
        return new AgentChips(questions, ideas.FirstOrDefault() ?? first);
    }
}
