using LouisAgent.Choices;

namespace LouisAgent.Evals.Files;

/// <summary>
/// The things code can tell for certain about a turn, each by the first word an eval file writes it with
/// (ADR 0024: a choice is an enum, read once).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public enum FactKind
{
    /// <summary>The turn ended without an error.</summary>
    [WireName("no-error")] NoError,

    /// <summary>The answer holds words the person reads, not only tool calls such as a card.</summary>
    [WireName("has-words")] HasWords,

    /// <summary>The agent called the tool in this turn.</summary>
    [WireName("used")] Used,

    /// <summary>The agent didn't call the tool in this turn.</summary>
    [WireName("not-used")] NotUsed,

    /// <summary>The turn took less than that many whole seconds.</summary>
    [WireName("under")] Under,

    /// <summary>The answer as the agent sent it doesn't hold the text, the case ignored.</summary>
    [WireName("never-says")] NeverSays,

    /// <summary>In this turn the agent asked the agent at that address.</summary>
    [WireName("routed-to")] RoutedTo,

    /// <summary>It asked that agent in this turn, and in an earlier turn of the conversation too.</summary>
    [WireName("continued")] Continued
}
