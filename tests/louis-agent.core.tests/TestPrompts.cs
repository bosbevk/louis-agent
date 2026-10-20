using LouisAgent.Core.Skills;
using Microsoft.Extensions.FileProviders;

// CL15 step 3: core embeds no specialist, no language mode and no minion once Puls's files leave, so its tests bring
// their own roles (Prompts/: a product with a shared voice and two languages, a specialist with a toolsets header, a
// minion base and a minion role) and register them before the first prompt is read, as a host's Build does.
[SetUpFixture]
public class TestPrompts
{
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([new ManifestEmbeddedFileProvider(typeof(TestPrompts).Assembly, "Prompts")]);
}
