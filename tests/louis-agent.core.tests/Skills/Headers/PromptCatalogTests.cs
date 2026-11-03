using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;

namespace LouisAgent.Core.Tests.Skills.Headers;

/// <summary>
/// CL27: every built-in prompt's header is read once, by <see cref="PromptCatalog"/>, and everything that needs it asks
/// there: the loader for the body the model reads and a role's toolsets (<c>SkillLoaderHeadersTests</c>), the portal
/// for the list.
/// </summary>
public class PromptCatalogTests
{
    /// <summary>The catalogue holds every built-in prompt, each with its header and body.</summary>
    [Test]
    public void TheCatalogue_HoldsEveryBuiltInPrompt_WithItsHeader()
    {
        var specialist = PromptCatalog.Get("specialists/testprod-specialist.md");

        Assert.That(PromptCatalog.All.Select(p => p.Path), Does.Contain("shared/principles.md"));
        Assert.That(PromptCatalog.All.Select(p => p.Path), Has.None.EndsWith("README.md"), "a folder's README is no prompt");
        Assert.That((specialist?.Header?.Name, specialist?.Header?.Level),
            Is.EqualTo(("testprod-specialist", (RoleLevel?)RoleLevel.Specialist)));
        Assert.That(specialist!.Body, Does.StartWith("# Testprod specialist"));
    }
}
