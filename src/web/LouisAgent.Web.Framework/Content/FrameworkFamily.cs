using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Web.Framework.Content;

/// <summary>
/// One of the framework's five families: its folder, what it is in a line, its icon, the colour it wears wherever the
/// site talks about it (Louis: "give each one a color this color we can then reuse talking about it"), and every
/// project it ships with a line of what that project is, as the repo map and the projects' index say.
/// </summary>
/// <remarks>
/// One record per family, named once here (ADR 0123, ask the type): a picture or a page asks the family for its
/// projects and its colour, so the engine can't be violet on one page and blue on the next, nor a project named on
/// one page and missing from another.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record FrameworkFamily(
    string Id,
    string Name,
    string Folder,
    string Summary,
    DiagramIcon Icon,
    DiagramTone Tone,
    IReadOnlyList<FrameworkProject> Projects)
{
    /// <summary>The engine: the loop, its guards, the providers and the ledger.</summary>
    public static readonly FrameworkFamily Engine = new("the-engine", "The engine", "src/engine",
        "Runs the agent, with the loop, its guards, the model providers and the usage ledger.",
        DiagramIcon.Chip, DiagramTone.Violet,
        [
            new("LouisAgent.Core", "The loop, guards, skills and usage."),
            new("LouisAgent.Composition", "The contracts everything agrees on."),
            new("LouisAgent.Choices", "One parser for a choice's name."),
            new("LouisAgent.SessionStream", "A session's events, read into records."),
            new("LouisAgent.Core.Web", "The browser, search and robots."),
            new("LouisAgent.Core.Telemetry", "The tracer and the collector's sink."),
            new("LouisAgent.Core.Anthropic", "Anthropic's models."),
            new("LouisAgent.Core.OpenAi", "OpenAI-compatible models."),
            new("LouisAgent.Core.Ollama", "Local models, through Ollama.")
        ]);

    /// <summary>The hosts, which put the agent in front of people.</summary>
    public static readonly FrameworkFamily Hosts = new("the-hosts", "The hosts", "src/hosts",
        "Put the agent in front of people, in the terminal, in Rider, over an HTTP API or MCP.",
        DiagramIcon.Server, DiagramTone.Amber,
        [
            new("LouisAgent.Cli", "The terminal."),
            new("LouisAgent.Acp", "Rider, with its tool discovery."),
            new("LouisAgent.Api", "Sessions and their stream, and a web app served."),
            new("LouisAgent.Api.Contracts", "The API's replies and bodies, as records."),
            new("LouisAgent.Mcp", "The tools over MCP, with no model."),
            new("LouisAgent.Accounts", "Sign-in, registration and a profile."),
            new("LouisAgent.Accounts.Contracts", "The accounts' records.")
        ]);

    /// <summary>The web, what every site is built on.</summary>
    public static readonly FrameworkFamily Web = new("the-web", "The web", "src/web",
        "What every site is built on, its frames and parts, themes, docs and pictures.",
        DiagramIcon.Browser, DiagramTone.Teal,
        [
            new("LouisAgent.Web", "What every site shares."),
            new("LouisAgent.Web.Themes", "The themes, glass and flat."),
            new("LouisAgent.Web.Accounts", "The account pages."),
            new("LouisAgent.Web.Docs", "The docs' parts, from links to the menu."),
            new("LouisAgent.Web.Places", "Every place on a page, its own address."),
            new("LouisAgent.Web.Framework", "These pages."),
            new("LouisAgent.Diagrams", "What a diagram is, its kinds and records."),
            new("LouisAgent.Diagrams.Engine", "What draws one, its layouts and drawings.")
        ]);

    /// <summary>The evals, which measure an agent, being rebuilt from scratch from their first project.</summary>
    public static readonly FrameworkFamily Evals = new("the-evals", "The evals", "src/evals",
        "Measure an agent. They are being rebuilt from scratch, starting with the eval file and how a turn is graded.",
        DiagramIcon.Checks, DiagramTone.Green,
        [
            new("LouisAgent.Evals", "The eval file and how a turn is graded.")
        ]);

    /// <summary>The tools, which draw the docs' pictures and read a provider's usage.</summary>
    public static readonly FrameworkFamily Tools = new("the-tools", "The tools", "src/tools",
        "Draw the docs' pictures and the site's films, and read what a model was paid.",
        DiagramIcon.Settings, DiagramTone.Blue,
        [
            new("LouisAgent.UsageProbe", "A provider's raw usage."),
            new("LouisAgent.FilmExport", "A site's films as WebP."),
            new("LouisAgent.DocDiagrams", "The docs' pictures from their JSON.")
        ]);

    /// <summary>The five, in the order the site tells them.</summary>
    public static readonly IReadOnlyList<FrameworkFamily> All = [Engine, Hosts, Web, Evals, Tools];
}
