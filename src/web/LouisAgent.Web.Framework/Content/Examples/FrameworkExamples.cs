using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tones;
using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content.Examples;

/// <summary>
/// The agents the framework already runs, and what's coming (F164.1, step 12): Puls, the coding agent and the guide,
/// each from its identity's own summary and its catalogue, and what's next from the plans in TODO.
/// </summary>
/// <remarks>
/// The page's lists as data, so the page only configures the framework's parts (ADR 0125).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public static class FrameworkExamples
{
    /// <summary>
    /// Puls, a South African general assistant, the first product built on the framework from outside: in a
    /// repository of its own, on the packages (F166.4, Louis's pick A of finding 1).
    /// </summary>
    public static readonly FrameworkExample Puls = new("Puls",
        ExampleHome.Elsewhere("bosbevk/puls", "src/Puls.Identity"),
        DiagramIcon.Dining, DiagramTone.Green, "A South African general assistant, its recipes and shop prices.",
    [
        new(["Puls.Packs.Recipes"], "Finds a recipe, read from a page's own schema.org data."),
        new(["Puls.Packs.ShopPrices"], "Works out what a shopping list costs, from Stats SA's average prices."),
        new(["Puls.Packs.WhereToBuy"], "Finds a shop's branches near your town, from OpenStreetMap."),
        new(["subagents"], "Five subagents, each with an address of its own that Puls can ask.")
    ]);

    /// <summary>The coding agent, in Rider and the terminal.</summary>
    public static readonly FrameworkExample Coding = new("The coding agent",
        ExampleHome.Here("demos/louis-agent/identity/LouisAgent.Identities.Coding"), DiagramIcon.Terminal,
        DiagramTone.Violet,
        "Works on code with you, in Rider and the terminal.",
    [
        new(["LouisAgent.Acp", "LouisAgent.Cli"],
            "Works in Rider's AI chat and in the terminal, from the same catalogue."),
        new(["LouisAgent.Packs.Files", "LouisAgent.Packs.Git", "LouisAgent.Packs.DotNet", "LouisAgent.Packs.Shell"],
            "Reads and writes the workspace, commits, builds and tests it, runs scripts."),
        new(["LouisAgent.Packs.Web", "LouisAgent.Packs.Docs"],
            "Searches the web, reads a page, and reads the project's own docs."),
        new(["LouisAgent.Identities.Triage"], "Beside it in the catalogue, a team that triages a service's errors.")
    ]);

    /// <summary>The guide on louis-agent.dev.</summary>
    public static readonly FrameworkExample Guide = new("The guide",
        ExampleHome.Here("src/site/LouisAgent.Identities.Guide"),
        DiagramIcon.Browser, DiagramTone.Teal, "Answers visitors in louis-agent.dev's corner, and shows them round.",
    [
        new(["louis-agent.dev"], "The guide in the site's corner, who answers about the framework from its docs."),
        new(["LouisAgent.Packs.Site"], "Takes a visitor to a page of the site, and to a place on it."),
        new(["LouisAgent.Packs.Leads"], "Tells the site's owner when someone is interested."),
        new(["LouisAgent.Identities.Showroom"],
            "The showroom's made-up businesses, a restaurant, a law practice and a guesthouse, each with its agent.")
    ]);

    /// <summary>The examples, in the order the page shows them.</summary>
    public static readonly IReadOnlyList<FrameworkExample> All = [Puls, Coding, Guide];

    /// <summary>
    /// Where louis-agent lives, and what's coming, in the order it is being built: the framework's site now, then the
    /// docs, then the MCP server (Louis, 2026-11-17).
    /// </summary>
    public static readonly IReadOnlyList<Term> Coming =
    [
        new(["louis-agent.dev"], "The public site, live. Websites with an agent, and the showroom."),
        new(["framework.louis-agent.dev"], "This site, for developers. Being built now."),
        new(["docs.louis-agent.dev"], "Every doc of the source, in full. Next."),
        new(["mcp://louis-agent"], "louis-agent over MCP, for your own client. After the docs."),
        new(["the evals"], "Reworked, and back on Cost and evals.")
    ];
}
