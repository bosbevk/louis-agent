using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Composition.Web.Search;

namespace LouisAgent.Composition.Packs;

/// <summary>
/// What the running agent offers a pack's tools when it builds them. The engine implements it, and a pack sees only this.
/// </summary>
/// <remarks>
/// Separated Interface (Fowler, PoEAA): the contract sits apart from what implements it, so a pack needs this small
/// package and not the whole engine.
/// </remarks>
public interface IPackContext
{
    /// <summary>The folder the agent works in. A tool that touches files stays inside it.</summary>
    string WorkspaceRoot { get; }

    /// <summary>The headless browser, when this instance has one.</summary>
    IWebBrowser? Browser { get; }

    /// <summary>The web search this instance uses.</summary>
    IWebSearchProvider WebSearch { get; }
}
