using System.ComponentModel;

namespace LouisAgent.Evals.Files;

/// <summary>
/// One conversation a person has with the agent: its id, the turns they take, and the page they are on.
/// </summary>
/// <param name="Id">The conversation's name, used once in its file.</param>
/// <param name="Turns">The turns the person takes, in order.</param>
/// <param name="Page">
/// The page a site's guide is on, an address starting with <c>/</c>; none for an agent off a site.
/// </param>
public sealed record Conversation(
    [property: Description("The conversation's name, used once in the file.")]
    string Id,
    [property: Description("The turns the person takes, in order.")]
    IReadOnlyList<Turn> Turns,
    [property: Description("The page the person is on when the agent is a site's guide, starting with /.")]
    string? Page = null);
