using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tones;
using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content.Features;

/// <summary>
/// Everything the framework does, by area (F164.1, step 11, Louis: "research all our features and list them this is
/// why people would want it"): the engine, the models, where it runs, the web, its guards, what it measures and the
/// code, each feature written from the summary of the code that does it, and said as what it gives you.
/// </summary>
/// <remarks>
/// The page's list as data, so the page only configures the framework's parts (ADR 0125). Each doc is named once, and
/// the features told in it point at it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public static class FrameworkFeatures
{
    /// <summary>The areas, each a section of the page, each in a tone of its own.</summary>
    public static readonly FeatureArea Engine = new("the-engine", "The engine", DiagramIcon.Chip, DiagramTone.Violet),
        Models = new("the-models", "The models", DiagramIcon.Cloud, DiagramTone.Blue),
        Hosts = new("the-hosts", "Where it runs", DiagramIcon.Server, DiagramTone.Amber),
        Web = new("the-web", "The web", DiagramIcon.Browser, DiagramTone.Teal),
        Guards = new("the-guards", "Its guards", DiagramIcon.Shield, DiagramTone.Red),
        Cost = new("the-cost", "What it measures", DiagramIcon.Chart, DiagramTone.Green),
        // How the code is written: kept, but its entries are rules, not features (Louis: "this is not a feature",
        // then "keep it").
        Code = new("the-code", "The code", DiagramIcon.Code, Counts: "rules");

    /// <summary>The areas, in the order the page tells them.</summary>
    public static readonly IReadOnlyList<FeatureArea> Areas = [Engine, Models, Hosts, Web, Guards, Cost, Code];

    /// <summary>The docs the features are told in: each one's path in the repo.</summary>
    private const string Projects = "src/docs/projects/",
        CoreDoc = Projects + "LouisAgent.Core.md",
        CoreWebDoc = Projects + "LouisAgent.Core.Web.md",
        TelemetryDoc = Projects + "LouisAgent.Core.Telemetry.md",
        ApiDoc = Projects + "LouisAgent.Api.md",
        CliDoc = Projects + "LouisAgent.Cli.md",
        AcpDoc = Projects + "LouisAgent.Acp.md",
        McpDoc = Projects + "LouisAgent.Mcp.md",
        AccountsDoc = Projects + "LouisAgent.Accounts.md",
        WebDoc = Projects + "LouisAgent.Web.md",
        DiagramsDoc = Projects + "LouisAgent.Diagrams.md",
        Adr = "src/docs/adr/";

    /// <summary>Every feature, area by area.</summary>
    public static readonly IReadOnlyList<FrameworkFeature> All =
    [
        new(Engine, "Only the tools it needs", "ToolsetCatalog",
            "A session names the toolsets it needs and nothing else loads, so the model sees only the tools it should.",
            Adr + "0006-toolsets-per-session.md"),
        new(Engine, "Skills you can read", "SkillLoader",
            "The system prompt is put together from markdown files, the principles first, so you change an agent by " +
            "editing text.", Adr + "0011-prompts-are-content.md"),
        new(Engine, "Agents that ask agents", "AskAgentTools",
            "An agent asks another by its address, a whole identity such as `acme` or one of its subagents such as " +
            "`acme.cook`, up to three asks a turn, and the answer comes back under a line that names who gave it. A " +
            "team of subagents is a few addresses, not a workflow to draw.",
            Adr + "0031-orchestration-is-an-engine-function.md"),
        new(Engine, "Follow-ups that remember", "SubSessionRegistry",
            "In a chat on the API, a subagent's own chat stays open after it answers, so the next question to it " +
            "carries on where it left off, with the files you attached passed to it by name.", CoreDoc),
        new(Engine, "Small jobs on a cheap model", "LOOKUP_LLMS",
            "A lookup agent does one task with one of its asker's tools, on the model you set for it, " +
            "cheapest first, a local one if you like.", CoreDoc),
        new(Engine, "An agent that grows its skills", "CreateSkill",
            "The agent can write a new skill, proved by an example run, and use it at once in the chat that made " +
            "it; every later chat has it once you /approve it.", "src/docs/architecture/06-runtime-view.md"),
        new(Engine, "Files passed by name", "SessionAttachments",
            "A file you attach is handed to another agent by name, so a large file isn't copied through the model.",
            CoreDoc),
        new(Engine, "Paced words", "STREAM_PACE",
            "A turn's text leaves the engine at a steady pace in whole words, not in the model's bursts.", CoreDoc),

        new(Models, "Any model, one setting", "LlmClientFactory",
            "Anthropic, any OpenAI-compatible endpoint, or a local model through Ollama, picked by LLM_PROVIDER.",
            Adr + "0003-provider-strategies.md"),
        new(Models, "Prompt caching", "IPromptCachingStrategy",
            "Each provider's prompt cache is used its own way, so a long chat pays less for what it has sent before.",
            CoreDoc),
        new(Models, "Replays for nothing", "ModelReplayChatClient",
            "A run's model answers are recorded, so the same requests replay with the same answers and cost nothing.",
            CoreDoc),

        new(Hosts, "The terminal", "CliHost", "A chat with your agent in the terminal, from two lines of your own.",
            CliDoc),
        new(Hosts, "Rider", "AcpHost", "Your agent inside Rider's AI chat.", AcpDoc),
        new(Hosts, "An HTTP API", "SessionEndpoints",
            "Sessions over HTTP, each turn streamed back as server-sent events while it is written.", ApiDoc),
        new(Hosts, "Paced once", "Stream-Paced",
            "The answer's header says the engine already paced its words, so a page leaves its own pacer off.", ApiDoc),
        new(Hosts, "MCP", "McpHost",
            "Another client brings its own model and calls your tools, and reads the packs' feeds as resources.",
            McpDoc),
        new(Hosts, "Accounts", "AccountsStartup",
            "Tenants, people and sign-in as building blocks, and nothing switched on until you name a database.",
            AccountsDoc),

        new(Web, "A site in a few lines", "AddSiteAreas",
            "A site names the areas it wants, and the framework wires them, so a site holds only its own words and " +
            "data.",
            Adr + "0120-nothing-is-hand-wired-from-the-framework.md"),
        new(Web, "A chat in one tag", "ChatWidget",
            "A chat any page drops in, folded in a corner, opened as the agent the page names.", WebDoc),
        new(Web, "The guide's caption", "AgentCaption",
            "The agent's last answer as a card with two sides, its words on the front and how it was made on the back.",
            WebDoc),
        new(Web, "The answer's motion", "CaptionHandOver",
            "An answer the guide's bar stops showing doesn't vanish. It fades, and goes once its caption has left.",
            WebDoc),
        new(Web, "Cards the agent draws", "DrawnCard",
            "The agent writes a card, such as a ticket, and the page draws it in the site's own look.", WebDoc),
        new(Web, "Every place an address", "PlacesArea",
            "Every section, card and page of a site has an address of its own, so the guide can take a visitor there.",
            Adr + "0107-every-section-has-an-address.md"),
        new(Web, "Found by search engines", "SiteRoutes",
            "The host draws each page as HTML for a search engine, before the app takes over in the browser.", ApiDoc),
        new(Web, "The guide knows where it is", "SiteFocusesBuilder",
            "Each part of a site tells the guide how to lead there, selling on one page and guiding on another.",
            WebDoc),
        new(Web, "An admin with an overview", "OverviewCountsBuilder",
            "What the site's agents did, counted by the tools the site cares about, such as its leads.", WebDoc),
        new(Web, "A pack's data as screens", "PackViews",
            "A pack's data in the admin as a plain table, unless the site draws a view of its own.", WebDoc),
        new(Web, "Reset my session", "SessionReset",
            "Puts the browser back to a first visit, every saved chat ended, as a new visitor sees the site.", WebDoc),
        new(Web, "Two themes, light and dark", "LookSwitch",
            "Glass and flat, each light and dark. The visitor picks, and the browser remembers.",
            Adr + "0124-a-sites-look-is-a-theme-and-a-style.md"),
        new(Web, "Pictures from data", "DiagramRenderers",
            "Flow, sequence, class, cards, nested and composite pictures from JSON, in the site's look and fonts.",
            DiagramsDoc),

        new(Guards, "A guarded context", "ToolCallGuards",
            "A tool's result over 50,000 characters is summarised, and a call the output limit cut short is never run.",
            Adr + "0005-guard-the-context.md"),
        new(Guards, "It takes a site's no", "RobotsWebBrowser",
            "Every fetch asks robots.txt first, and a page that says no to AI readers is refused.",
            Adr + "0068-every-fetch-obeys-robots-txt.md"),
        new(Guards, "A page's own data first", "JSON-LD",
            "A page's own structured data is read in code before its text, so a fact isn't guessed from prose.",
            Adr + "0067-a-pages-own-data-before-its-text.md"),
        new(Guards, "Four search engines", "WebSearchProviderKind",
            "Tavily, Google, SearXNG or DuckDuckGo, picked by WEB_SEARCH_PROVIDER.", CoreWebDoc),

        new(Cost, "Every request priced", "UsageLedger",
            "Every request's tokens and cost, priced from a dated table, a file a month, and unknown is never nought.",
            Adr + "0007-measure-every-request.md"),
        new(Cost, "A line per turn", "TurnLog",
            "One line for each finished turn beside the ledger, which the admin's overview reads back.", ApiDoc),
        new(Cost, "Traced end to end", "OtlpTelemetry",
            "Every model request, tool call and web request traced to the OpenTelemetry collector you name.",
            TelemetryDoc),

        new(Code, "A choice is an enum", "SettingChoice",
            "A setting that names one of a few choices is an enum, read once where it comes in.",
            Adr + "0024-choices-are-enums.md"),
        new(Code, "A name says what it is", "turn.Paced(clock)",
            "A method is a verb, a stream operator is named for what it gives back, a type is a noun with no empty " +
            "word.",
            Adr + "0126-a-name-says-what-it-is-by-its-kind.md")
    ];

    /// <summary>An area's features, as its term list shows them.</summary>
    public static IReadOnlyList<Term> In(FeatureArea area) =>
        [.. All.Where(feature => feature.Area == area).Select(feature => feature.Term)];
}
