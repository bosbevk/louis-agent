using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Core.Tools.Web.Rendering.Setups;

/// <summary>
/// One of these per page renderer. It knows whether the renderer is set up and how to build it. If it isn't set up you get a renderer that tells the model what to set, not an error.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object. "Is it ready?" and "build it" live in the same class, so they can't drift apart, and a new renderer is one new class instead of another case in two switches.
/// A setup that isn't ready hands back a renderer that explains what's missing, so nothing has to check for null and the model still gets a useful answer.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public interface IPageRendererSetup
{
    /// <summary>Which renderer this is.</summary>
    PageRendererKind Kind { get; }

    /// <summary>We can only use the renderer if its settings are there.</summary>
    bool IsReady(AgentOptions options);

    /// <summary>
    /// Builds the renderer. If it isn't set up, you get one that tells the model what to set.
    /// Chrome reads through the one guarded browser it's given, so a recorded run holds every page.
    /// </summary>
    IPageRenderer Create(AgentOptions options, HttpClient? http, IWebBrowser? browser);
}
