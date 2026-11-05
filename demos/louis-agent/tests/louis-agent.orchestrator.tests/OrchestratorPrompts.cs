using LouisAgent.Core.Skills;
using LouisAgent.Identities.Coding;

namespace LouisAgent.Orchestrator.Tests;

/// <summary>The coding identity's prompts, registered as the orchestrator's program does when it starts.</summary>
[SetUpFixture]
public class OrchestratorPrompts
{
    /// <summary>Registers them before any test reads a prompt.</summary>
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([new CodingIdentity().Prompts]);
}
