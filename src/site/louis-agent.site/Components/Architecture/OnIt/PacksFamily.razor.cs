using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Site.Components.Architecture.OnIt;

/// <summary>The packs on the Architecture page: the four folders a pack is, and every project in them.</summary>
public partial class PacksFamily
{
    /// <summary>Each folder of the packs, and what is in it.</summary>
    private static readonly IReadOnlyList<Term> Terms =
    [
        new(["tools/"], "`louis-agent.pack.files` · `louis-agent.pack.git` · `louis-agent.pack.dotnet` · `louis-agent.pack.shell` · `louis-agent.pack.web` · `louis-agent.pack.docs` · `louis-agent.pack.triage` · `louis-agent.pack.leads` · `louis-agent.pack.site` · `louis-agent.pack.paymo` · `louis-agent.pack.devops` · `louis-agent.pack.calculator` · `louis-agent.pack.where-to-buy` · `louis-agent.pack.shop-prices` · `louis-agent.pack.recipes`"),
        new(["evals/"], "`louis-agent.pack.recipes.checks` · `louis-agent.pack.where-to-buy.checks`: the rules an eval names to prove a pack's answer, apart from the tools, so only the runner references them."),
        new(["web/"], "`louis-agent.pack.files.web` · `louis-agent.pack.git.web` · `louis-agent.pack.recipes.web` · `louis-agent.pack.triage.web`, the triage demo: a pack's own screens for the sites that use them, on the framework's web."),
        new(["tests/"], "One test project per pack, and an integration test where a pack leaves its process.")
    ];
}
