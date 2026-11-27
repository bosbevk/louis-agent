using LouisAgent.Core.Prompting;
using LouisAgent.Packs.Docs;
using LouisAgent.Packs.Site;

namespace LouisAgent.Identities.Framework.Tests;

/// <summary>
/// Registers the prompts the framework Orbi and the packs it names bring, before any test reads the first one, as a
/// host's start-up does with its catalogue's (CL15).
/// </summary>
[SetUpFixture]
public class FrameworkPrompts
{
    /// <summary>The identity's prompts, and its two packs'.</summary>
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([
        new FrameworkIdentity().Prompts, new DocsPack(new DocsOptions { Root = "test-docs" }).Prompts,
        new SitePack().Prompts
    ]);
}
