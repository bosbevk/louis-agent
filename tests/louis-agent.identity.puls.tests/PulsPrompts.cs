using LouisAgent.Core.Skills;
using LouisAgent.Identities.Puls;

// CL15: a test assembly registers the prompts its identity brings before the first is read, as a host's Build does.
[SetUpFixture]
public class PulsPrompts
{
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([new PulsIdentity().Prompts]);
}
