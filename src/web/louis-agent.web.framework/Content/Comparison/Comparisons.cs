using LouisAgent.Web.Components.Site.Tables;
using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content.Comparison;

/// <summary>
/// louis-agent beside Microsoft Agent Framework and Semantic Kernel (F160.1, step 12, ADR 0110): what louis-agent does,
/// each held to a file of the repo, and what the others' own pages say, read on <see cref="ReadOn"/>, nothing more.
/// Where their pages say nothing of a topic, the cell says so, and never that they lack it.
/// </summary>
/// <remarks>
/// The page's rows as data, so the page only configures the framework's parts (ADR 0125).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0110-a-page-says-what-it-is-in-one-head.md">ADR 0110: a page says what it is in one head</seealso>
public static class Comparisons
{
    /// <summary>The day the others' pages were read.</summary>
    public const string ReadOn = "2026-11-17";

    /// <summary>What a cell says when the others' pages say nothing of a topic.</summary>
    private const string NotStated = "Not stated";

    /// <summary>The three, across the top.</summary>
    public static readonly IReadOnlyList<string> Columns =
        ["louis-agent", "Microsoft Agent Framework", "Semantic Kernel"];

    /// <summary>Every row.</summary>
    public static readonly IReadOnlyList<ComparisonClaim> All =
    [
        // Where it runs comes first: it is louis-agent's to be proud of (Louis: "this is a selling point").
        new("Where it runs",
            "On your own hardware. Docker on your own machine or server, no cloud agent service in between, and a " +
            "local model through Ollama keeps every word on it", "demos/louis-agent/docker/docker-compose.yml",
            "Quickstarts on Microsoft Foundry, Foundry hosted agents", "Local deployment"),
        new("Languages", "C#, end to end, with its websites in Blazor", "louis-agent-solution.sln",
            ".NET, Python, and Go in public preview", "C#, Python and Java"),
        new("Built on", "Microsoft.Extensions.AI, one chat client for every provider",
            "src/docs/adr/0002-messages-api-tier-with-our-own-loop.md", NotStated, NotStated),
        new("Tools", "Packs of tools, served to any client over MCP", "src/hosts/louis-agent.mcp/McpHost.cs",
            "Tools and MCP servers", "Plugins, OpenAPI and MCP"),
        new("Several agents", "Agents ask agents by their address, an identity or one of its roles, and a " +
            "specialist remembers the chat for a follow-up",
            "src/engine/louis-agent.core/Delegation/Agents/AskAgentTools.cs",
            "Graph-based workflows, sequential, concurrent, handoff and group", "Collaborating agents, Process Framework"),
        new("Local models", "Ollama", "src/engine/louis-agent.core.ollama/louis-agent.core.ollama.csproj",
            "Ollama among its providers", "Ollama, LMStudio or ONNX"),
        new("Tracing", "OpenTelemetry", "src/engine/louis-agent.core.telemetry/OtlpTelemetry.cs",
            "Built-in OpenTelemetry", "Telemetry, hooks and filters"),
        new("What a request cost", "Every request priced in a ledger, shown in your currency",
            "src/engine/louis-agent.core/Hosting/UsageLedger.cs", NotStated, NotStated),
        new("A website with the agent", "Sites on louis-agent.web, the guide in the corner",
            "src/docs/projects/louis-agent.web.md", NotStated, NotStated),
        new("Where it comes from", "Its own loop on Microsoft.Extensions.AI, not on Agent Framework",
            "src/docs/adr/0030-build-or-adopt.md",
            "The direct successor to Semantic Kernel and AutoGen", "Now Microsoft Agent Framework, its README says")
    ];

    /// <summary>The rows as the table draws them.</summary>
    public static IReadOnlyList<ComparisonRow> Rows => [.. All.Select(claim => claim.Row)];

    /// <summary>The others' own pages, each linked, which their cells were read from.</summary>
    public static readonly IReadOnlyList<(string Name, string Address)> Sources =
    [
        ("The Agent Framework overview", "https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview"),
        ("Agent Framework on GitHub", "https://github.com/microsoft/agent-framework"),
        ("The Semantic Kernel overview", "https://learn.microsoft.com/en-us/semantic-kernel/overview/"),
        ("Semantic Kernel on GitHub", "https://github.com/microsoft/semantic-kernel")
    ];

    /// <summary>The sources as a term list shows them, each its link.</summary>
    public static IReadOnlyList<Term> SourceTerms =>
        [.. Sources.Select(source => new Term([ReadOn], $"[{source.Name}]({source.Address})"))];
}
