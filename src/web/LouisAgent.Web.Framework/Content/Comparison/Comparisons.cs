using LouisAgent.Composition.Words;
using LouisAgent.Web.Components.Site.Tables;
using LouisAgent.Web.Components.Site.Text;
using LouisAgent.Web.Framework.Content.Comparison.Parts;

namespace LouisAgent.Web.Framework.Content.Comparison;

/// <summary>
/// louis-agent beside Microsoft Agent Framework and Semantic Kernel (F164.1, step 12, ADR 0110): what louis-agent does,
/// each held to a file of the repo, and what the others' own pages say, read on <see cref="ReadOn"/>, nothing more.
/// Where their pages say nothing of a topic, the cell says so, and never that they lack it.
/// </summary>
/// <remarks>
/// The page's rows as data, so the page only configures the framework's parts (ADR 0125), each list a dictionary
/// factory keyed by its parts (ADR 0132). The rows cover every part of the framework the others' pages also speak
/// to, so the site's plugin architecture shows beside theirs (Louis's pick of three, 2026-11-22, over the plugin rows
/// alone and over every part, where a row would read "Not stated" twice).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0110-a-page-says-what-it-is-in-one-head.md">ADR 0110: a page says what it is in one head</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0132-words-come-from-a-dictionary-factory.md">ADR 0132: words come from a dictionary factory</seealso>
public static class Comparisons
{
    /// <summary>The day the others' pages were read.</summary>
    public const string ReadOn = "2026-11-22";

    /// <summary>What a cell says when the others' pages say nothing of a topic.</summary>
    private const string NotStated = "Not stated";

    /// <summary>The three, across the top.</summary>
    public static readonly IReadOnlyList<string> Columns =
        ["louis-agent", "Microsoft Agent Framework", "Semantic Kernel"];

    /// <summary>Every row, by its topic.</summary>
    public static readonly PartWords<ComparisonTopic, ComparisonClaim> Claims =
        PartWords<ComparisonTopic, ComparisonClaim>.Of(topic => topic switch
        {
            // Where it runs comes first: it is louis-agent's to be proud of (Louis: "this is a selling point").
            ComparisonTopic.WhereItRuns => new ComparisonClaim("Where it runs",
                "On your own hardware. Docker on your own machine or server, no cloud agent service in between, " +
                "and a local model through Ollama keeps every word on it", "demos/louis-agent/docker/docker-compose.yml",
                "Quickstarts on Microsoft Foundry, Foundry hosted agents", "Local deployment"),
            // What it takes to start comes second: its ease of use is the framework's core (Louis, 2026-11-23), told
            // as what each one's first sample needs, over a row on switching models and a count of Foundry's steps.
            ComparisonTopic.WhatTheFirstAnswerNeeds => new ComparisonClaim("What the first answer needs",
                "One provider's key in a settings file, or no key at all with a local model. No cloud account, no " +
                "model deployment, no role to assign", "demos/louis-agent/config/.env.ollama",
                "Its first C# sample signs in to Azure OpenAI with the Azure CLI",
                "Its first .NET sample reads an Azure OpenAI deployment, endpoint and key"),
            ComparisonTopic.Languages => new ComparisonClaim("Languages", "C#, end to end, with its websites in Blazor",
                "louis-agent-solution.sln", "Python and C#/.NET, Go in a repo of its own", "C#, Python and Java"),
            ComparisonTopic.BuiltOn => new ComparisonClaim("Built on",
                "Microsoft.Extensions.AI, one chat client for every provider",
                "src/docs/adr/0002-messages-api-tier-with-our-own-loop.md", NotStated, NotStated),
            ComparisonTopic.HowYouExtendIt => new ComparisonClaim("How you extend it",
                "An engine, identities and packs. An identity is who an agent is, a pack is what it can do, and you " +
                "pick both at start-up", "src/docs/adr/0027-engine-identities-and-packs.md",
                "Middleware, context providers and MCP clients",
                "Plugins from native code, prompt templates, OpenAPI or MCP"),
            ComparisonTopic.Tools => new ComparisonClaim("Tools", "Packs of tools, served to any client over MCP",
                "src/hosts/LouisAgent.Mcp/McpHost.cs", "Tools and MCP servers", "Plugins, OpenAPI and MCP"),
            ComparisonTopic.Prompts => new ComparisonClaim("Prompts",
                "Markdown files you read and edit. The system prompt is put together from them, the principles first",
                "src/docs/adr/0011-prompts-are-content.md", "Declarative agents in YAML, Agent Skills",
                "Prompt templates"),
            ComparisonTopic.SeveralAgents => new ComparisonClaim("Several agents",
                "Agents ask agents by their address, an identity or one of its subagents, and a subagent remembers " +
                "the chat for a follow-up", "src/engine/LouisAgent.Core/Delegation/Agents/AskAgentTools.cs",
                "Graph-based workflows, sequential, concurrent, handoff and group",
                "Collaborating agents, Process Framework"),
            ComparisonTopic.WhereYouReachIt => new ComparisonClaim("Where you reach it",
                "The terminal, Rider's AI chat, an HTTP API that streams each turn, and MCP for a client that brings " +
                "its own model", "src/docs/projects/README.md",
                "A2A, AG-UI, Azure Functions, M365 and Foundry hosted agents", NotStated),
            ComparisonTopic.LocalModels => new ComparisonClaim("Local models", "Ollama",
                "src/engine/LouisAgent.Core.Ollama/LouisAgent.Core.Ollama.csproj", "Ollama among its providers",
                "Ollama, LMStudio or ONNX"),
            ComparisonTopic.SessionsAndMemory => new ComparisonClaim("Sessions and memory",
                "Sessions survive a restart, kept in MongoDB for a set time after their last message, and a " +
                "subagent's chat stays open for a follow-up",
                "src/hosts/LouisAgent.Api/Sessions/Store/ISessionStore.cs",
                "Session state, checkpointing and time-travel", "Memory and vector database support"),
            ComparisonTopic.Guards => new ComparisonClaim("Guards",
                "Built in: an oversized tool result is summarised, a cut-off call is never run, and every fetch asks " +
                "robots.txt first", "src/docs/adr/0005-guard-the-context.md", "Middleware, human-in-the-loop",
                "Hooks and filters"),
            ComparisonTopic.Tracing => new ComparisonClaim("Tracing", "OpenTelemetry",
                "src/engine/LouisAgent.Core.Telemetry/OtlpTelemetry.cs", "Built-in OpenTelemetry",
                "Telemetry, hooks and filters"),
            ComparisonTopic.WhatARequestCost => new ComparisonClaim("What a request cost",
                "Every request priced in a ledger, shown in your currency",
                "src/engine/LouisAgent.Core/Hosting/UsageLedger.cs", NotStated, NotStated),
            ComparisonTopic.AWebsiteWithTheAgent => new ComparisonClaim("A website with the agent",
                "Blazor sites built from parts. A site names its areas in one call, a pack brings its own screens, " +
                "and the guide sits in the corner", "src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md",
                "DevUI, a developer UI for testing and debugging", NotStated),
            ComparisonTopic.WhereItComesFrom => new ComparisonClaim("Where it comes from",
                "Its own loop on Microsoft.Extensions.AI, not on Agent Framework", "src/docs/adr/0030-build-or-adopt.md",
                "The direct successor to Semantic Kernel and AutoGen", "Now Microsoft Agent Framework, its README says"),
            _ => throw new ArgumentOutOfRangeException(nameof(topic), topic, null)
        });

    /// <summary>Every row, in the table's order.</summary>
    public static IReadOnlyList<ComparisonClaim> All => Claims.InOrder;

    /// <summary>The rows as the table draws them.</summary>
    public static IReadOnlyList<ComparisonRow> Rows => [.. All.Select(claim => claim.Row)];

    /// <summary>The others' own pages, each linked, which their cells were read from.</summary>
    public static readonly PartWords<ComparisonSource, ComparisonSourcePage> Sources =
        PartWords<ComparisonSource, ComparisonSourcePage>.Of(source => source switch
        {
            ComparisonSource.AgentFrameworkOverview => new ComparisonSourcePage("The Agent Framework overview",
                "https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview"),
            ComparisonSource.AgentFrameworkOnGitHub => new ComparisonSourcePage("Agent Framework on GitHub",
                "https://github.com/microsoft/agent-framework"),
            ComparisonSource.SemanticKernelOverview => new ComparisonSourcePage("The Semantic Kernel overview",
                "https://learn.microsoft.com/en-us/semantic-kernel/overview/"),
            ComparisonSource.SemanticKernelOnGitHub => new ComparisonSourcePage("Semantic Kernel on GitHub",
                "https://github.com/microsoft/semantic-kernel"),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        });

    /// <summary>The sources as a term list shows them, each its link.</summary>
    public static IReadOnlyList<Term> SourceTerms =>
        [.. Sources.InOrder.Select(source => new Term([ReadOn], $"[{source.Name}]({source.Address})"))];
}
