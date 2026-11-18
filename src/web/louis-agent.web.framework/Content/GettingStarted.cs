using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content;

/// <summary>
/// What Getting started tells beside its program (F162.1, step 9): whose is what, and the steps that run the coding
/// agent from its own settings, each written from the repo as it stands (the catalogue, the settings' order in
/// <c>EnvFile</c>, the demo's <c>config/</c> and its launch profile).
/// </summary>
/// <remarks>
/// The page's lists as data, so the page only configures the framework's parts (ADR 0125).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public static class GettingStarted
{
    /// <summary>What you write, and what you take from the framework.</summary>
    public static readonly IReadOnlyList<Term> WhoseIsWhat =
    [
        new(["Program.cs", "CodingCatalog"],
            "Yours. The program that starts a host, and the catalogue that names which identities and packs it loads."),
        new(["identity/", "config/"],
            "Yours too. Who your agent is, as markdown you can read, and the settings it runs with."),
        new(["louis-agent.core", "louis-agent.cli"],
            "The framework's. The engine that runs the loop, and the host that puts it in the terminal."),
        new(["packs/tools/"],
            "Ready-made packs. Each is what an agent can do, a package you name in your catalogue or leave out.")
    ];

    /// <summary>The steps from a fresh clone to the coding agent answering in the terminal.</summary>
    public static readonly IReadOnlyList<Step> RunIt =
    [
        new("Copy the settings.",
            "Copy `demos/louis-agent/config/.env.example` to `.env` beside it, and " +
            "`.env.secrets.example` to `.env.secrets`."),
        new("Name a model.",
            "Put `ANTHROPIC_API_KEY` in `.env.secrets`, or pick a local model through Ollama with `LLM_PROFILE=ollama`."),
        new("Run it.",
            "From `demos/louis-agent/hosts/louis-agent.coding-cli`, type `dotnet run`. Its launch profile points it at " +
            "the demo's `config/` folder.")
    ];

    /// <summary>The order the settings are read in, the first that sets a name winning.</summary>
    public static readonly IReadOnlyList<Term> SettingsOrder =
    [
        new(["your shell"], "What is set already wins, so one run can switch, as `LLM_PROFILE=ollama dotnet run` does."),
        new([".env.secrets"], "Your keys, kept out of git."),
        new([".env"], "The demo's settings."),
        new([".env.{LLM_PROFILE}"], "The model's own, such as `.env.ollama` or `.env.anthropic`, read last.")
    ];
}
