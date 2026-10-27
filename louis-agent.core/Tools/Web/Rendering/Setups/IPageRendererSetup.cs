using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Core.Tools.Web.Rendering.Setups;

/// <summary>
/// Strategy: one small class per page renderer that knows whether the renderer's settings are there (<see cref="IsReady"/>) and how to build it
/// (<see cref="Create"/>), so "ready" and "build" live together and cannot drift. <see cref="Create"/> always returns a working renderer, or the setup's own
/// Null Object (an <c>UnavailablePageRenderer</c> that says what to set) when the settings are missing; the factory never checks readiness itself.
/// </summary>
public interface IPageRendererSetup
{
    /// <summary>Which renderer this setup is for.</summary>
    PageRendererKind Kind { get; }

    /// <summary>Whether the settings this renderer needs are present in <paramref name="options"/>.</summary>
    bool IsReady(AgentOptions options);

    /// <summary>The renderer when it is ready; otherwise its Null Object, an unavailable renderer whose every call says what is missing. <paramref name="browser"/> is the process's one guarded browser, used by the Chrome renderer.</summary>
    IPageRenderer Create(AgentOptions options, HttpClient? http, IWebBrowser? browser);
}
