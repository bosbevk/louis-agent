using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;
using NUnit.Framework;

namespace LouisAgent.Testing;

/// <summary>The repo's paths and the options a test builds an engine with, the same from every test project.</summary>
public static class TestPaths
{
    /// <summary>The repo root, five levels above a test project's build output (<c>tests/{project}/bin/Debug/net10.0</c>).</summary>
    public static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    /// <summary>The built-in prompts in core (CL8 step 4), embedded and read only from the assembly.</summary>
    public static string Prompts { get; } = Path.Combine(RepoRoot, "louis-agent.core", "Prompts");

    public static AgentOptions Agent(string? paymoKey = null) => new()
    {
        WorkspaceRoot = RepoRoot,
        PaymoApiKey = paymoKey,
        // No network in tests: AgentHost.Build would otherwise fetch exchange rates from Frankfurter (F14).
        RatesUrl = null,
    };

    /// <summary>
    /// The tool objects of the named toolsets, built by the catalogue as a session's are (F5: an engine has only the
    /// toolsets it is given). <paramref name="webSearch"/> replaces the configured search provider.
    /// </summary>
    public static IReadOnlyList<object> Toolsets(AgentOptions options, IEnumerable<string> names, IWebSearchProvider? webSearch = null)
    {
        var context = new ToolsetContext(options, webSearch ?? WebSearchProviderFactory.Create(options));
        return ToolsetCatalog.BuildTools(ToolsetCatalog.Resolve(names, context), context);
    }
}
