using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content;

/// <summary>
/// What is built on the five families, each by its folder in the repo: the framework's own websites, the two demos
/// and the packs, so the Overview shows where a project of your own would sit (the repo map says what each holds).
/// </summary>
/// <remarks>
/// The page's list as data, so the page only configures the framework's parts (ADR 0125); the Overview's test holds
/// every folder named here to the disk.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public static class BuiltOnIt
{
    /// <summary>Each folder, and what it holds, in a line or two.</summary>
    public static readonly IReadOnlyList<Term> Folders =
    [
        new(["packs/tools"],
            "The ready-made packs, from files and git to the web, the docs and a calculator. Their tests and web " +
            "views sit beside them in folders of their own, so you can take the tools alone."),
        new(["src/site"],
            "The framework's own websites. louis-agent.dev with the guide in its corner, this site with Orbi beside " +
            "it, and the showroom's three made-up businesses."),
        new(["demos/louis-agent"],
            "The coding agent and the triage team, with the code-review web app and the programs that start them."),
        new(["demos/starter"],
            "The smallest site there is, and the template that `dotnet new louis-site` makes a new one from.")
    ];
}
