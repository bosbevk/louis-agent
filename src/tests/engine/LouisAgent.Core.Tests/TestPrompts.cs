using LouisAgent.Composition.Packs;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Tests.Hosting.Catalogue;

// CL15 step 3: core embeds no subagent, no language mode and no lookup agent once Puls's files leave, so its tests
// bring their own roles (agents/testprod/: a product with a shared voice and two languages, a subagent with a toolsets
// header, a lookup agents' base and a lookup agent) and register them before the first prompt is read, as a host's
// Build does; the made-up pack that reads a file (F171.1) registers its guide here for the same reason.
[SetUpFixture]
public class TestPrompts
{
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([
        EmbeddedPrompts.Of(typeof(TestPrompts).Assembly), TestTools.Pack.Prompts, TestTools.WebPack.Prompts,
        ReadSamplePack.Guides
    ]);
}
