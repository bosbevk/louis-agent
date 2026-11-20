using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content.Build;

/// <summary>
/// What Run it tells: the stacks that run on your own machine, the dashboard that watches them, and how the two
/// sites reach Azure, each written from the compose files' own comments, the docker README and
/// <c>deploy-the-sites.md</c>.
/// </summary>
/// <remarks>
/// The page's lists as data, so the page only configures the framework's parts (ADR 0125); the page's test holds every
/// compose file, service and release file named here to the repo.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public static class RunIt
{
    /// <summary>The coding agent's stack: its API and web app, Rider's two servers and the agent in a terminal.</summary>
    public static readonly ComposeStack Coding = new("demos/louis-agent/docker/docker-compose.yml",
        ["api", "acp-server", "mcp-server", "local-agent"],
        "docker compose -f demos/louis-agent/docker/docker-compose.yml " +
        "--env-file demos/louis-agent/config/.env up -d --build api",
        "The coding agent's API with its code-review app on port 5082, Rider's ACP and MCP servers, and the agent in a " +
        "terminal. A local model through Ollama joins under the `ollama`, `ollama-nvidia` or `ollama-amd` profile.");

    /// <summary>The framework's two websites, as they run in Azure.</summary>
    public static readonly ComposeStack Sites = new("src/site/docker/docker-compose.yml", ["site", "framework"],
        "docker compose -f src/site/docker/docker-compose.yml up -d --build",
        "louis-agent.dev on port 5080 and this site on 5084, from the same images that run in Azure.");

    /// <summary>The dashboard every stack and Rider send their logs and traces to.</summary>
    public static readonly ComposeStack Dashboard = new("demos/louis-agent/docker/docker-compose.observability.yml",
        ["dashboard"], "docker compose -f demos/louis-agent/docker/docker-compose.observability.yml up -d",
        "The Aspire dashboard. Its page is on port 18888, and the stacks send it their logs and traces over OTLP on " +
        "4317. It keeps everything in memory, so a restart forgets it.");

    /// <summary>Every stack the page names.</summary>
    public static readonly IReadOnlyList<ComposeStack> Stacks = [Coding, Sites, Dashboard];

    /// <summary>The stacks of your own machine, as their term list shows them.</summary>
    public static IReadOnlyList<Term> OnYourMachine => [Coding.Term, Sites.Term];

    /// <summary>The dashboard, as its term list shows it.</summary>
    public static IReadOnlyList<Term> Watching => [Dashboard.Term];

    /// <summary>The commands that start them, one to a line.</summary>
    public static string Commands => string.Join("\n", Coding.Command, Sites.Command);

    /// <summary>The files a release runs: the workflow every merge starts, and the script it runs.</summary>
    public static readonly IReadOnlyList<string> ReleaseFiles =
        [".github/workflows/deploy-sites.yml", "src/site/deploy/azure/deploy.ps1"];

    /// <summary>What is in Azure, and how it gets there.</summary>
    public static readonly IReadOnlyList<Term> InAzure =
    [
        new(["deploy-sites.yml"],
            "Every merge to `main` releases both sites from GitHub, so nobody's machine needs to be on."),
        new(["deploy.ps1"],
            "The one script that does every job. The workflow runs it, and you can run the same verbs from your own PC."),
        new(["Container Apps"],
            "Each site is one Azure Container App that sleeps when nobody visits. The first visitor after a quiet " +
            "spell waits a few seconds for it to wake.")
    ];
}
