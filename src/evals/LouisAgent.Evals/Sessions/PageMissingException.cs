using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Sessions;

namespace LouisAgent.Evals.Sessions;

/// <summary>
/// The page a conversation names is not a page of the site: the site's page route answered not found. It carries the
/// address, so a run that stops on it says which page.
/// </summary>
/// <param name="address">The address the conversation names.</param>
public sealed class PageMissingException(string address) : Exception(MessageOf(address))
{
    /// <summary>The address the conversation names.</summary>
    public string Address { get; } = address;

    /// <summary>The missing page said in the evals' words, with its address.</summary>
    private static string MessageOf(string address)
    {
        var missing = new PageMissingHoles(address);
        return MessageText.Format(SessionMessage.PageMissing, missing);
    }
}
