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
            new("louis-agent.core", "The loop, guards, skills and usage."),
            new("louis-agent.composition", "The contracts everything agrees on."),
            new("louis-agent.choices", "One parser for a choice's name."),
            new("louis-agent.session-stream", "A session's events, read into records."),
            new("louis-agent.core.web", "The browser, search and robots."),
            new("louis-agent.core.telemetry", "The tracer and the collector's sink."),
            new("louis-agent.core.anthropic", "Anthropic's models."),
            new("louis-agent.core.openai", "OpenAI-compatible models."),
            new("louis-agent.core.ollama", "Local models, through Ollama.")
        ]);

    /// <summary>The hosts, which put the agent in front of people.</summary>
    public static readonly FrameworkFamily Hosts = new("the-hosts", "The hosts", "src/hosts",
        "Put the agent in front of people, in the terminal, in Rider, over an HTTP API or MCP.",
        DiagramIcon.Server, DiagramTone.Amber,
        [
            new("louis-agent.cli", "The terminal."),
            new("louis-agent.acp", "Rider, with its tool discovery."),
            new("louis-agent.api", "Sessions and their stream, and a web app served."),
            new("louis-agent.api.contracts", "The API's replies and bodies, as records."),
            new("louis-agent.api.evals", "The eval studio's API."),
            new("louis-agent.mcp", "The tools over MCP, with no model."),
            new("louis-agent.accounts", "Sign-in, registration and a profile."),
            new("louis-agent.accounts.contracts", "The accounts' records.")
        ]);

    /// <summary>The web, what every site is built on.</summary>
    public static readonly FrameworkFamily Web = new("the-web", "The web", "src/web",
        "What every site is built on, its frames and parts, themes, docs and pictures.",
        DiagramIcon.Browser, DiagramTone.Teal,
        [
            new("louis-agent.web", "What every site shares."),
            new("louis-agent.web.themes", "The themes, glass and flat."),
            new("louis-agent.web.accounts", "The account pages."),
            new("louis-agent.web.evals", "The eval studio's pages."),
            new("louis-agent.web.docs", "The docs' parts, from links to the menu."),
            new("louis-agent.web.framework", "These pages."),
            new("louis-agent.diagrams", "What a diagram is, its kinds and records."),
            new("louis-agent.diagrams.engine", "What draws one, its layouts and drawings.")
        ]);

    /// <summary>The evals, which measure an agent.</summary>
    public static readonly FrameworkFamily Evals = new("the-evals", "The evals", "src/evals",
        "Measure an agent, with the harness, its protocol, the checks' contract and the runner.",
        DiagramIcon.Checks, DiagramTone.Green,
        [
            new("louis-agent.evals", "The harness."),
            new("louis-agent.evals.protocol", "Its protocol."),
            new("louis-agent.evals.checks", "The checks' contract."),
            new("louis-agent.evals.runner", "The runner.")
        ]);

    /// <summary>The tools, which draw the docs' pictures and read a provider's usage.</summary>
    public static readonly FrameworkFamily Tools = new("the-tools", "The tools", "src/tools",
        "Draw the docs' pictures and the site's films, and read what a model was paid.",
        DiagramIcon.Settings, DiagramTone.Blue,
        [
            new("louis-agent.usage-probe", "A provider's raw usage."),
            new("louis-agent.film-export", "A site's films as WebP."),
            new("louis-agent.doc-diagrams", "The docs' pictures from their JSON.")
        ]);

    /// <summary>The five, in the order the site tells them.</summary>
    public static readonly IReadOnlyList<FrameworkFamily> All = [Engine, Hosts, Web, Evals, Tools];
}
