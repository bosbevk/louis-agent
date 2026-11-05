using LouisAgent.Composition.Pricing;
using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Composition.Web.Rendering;
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

    /// <summary>What reads a page a browser has to draw; one that says so when this instance has none.</summary>
    IPageRenderer Renderer { get; }

    /// <summary>The day's exchange rates, when this instance loaded some.</summary>
    ICurrencyRates? CurrencyRates { get; }

    /// <summary>A fetcher with the engine's guards around it. One per toolset, shared by its tools.</summary>
    IPageFetcher NewFetcher();
}
