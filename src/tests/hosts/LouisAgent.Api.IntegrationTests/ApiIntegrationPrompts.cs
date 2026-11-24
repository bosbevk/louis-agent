using LouisAgent.Api.IntegrationTests.Sessions;
using LouisAgent.Core.Skills;

/// <summary>
/// The API's integration tests open a session on a made-up pack that reads a file (F171.1), so its guide is
/// registered before the first prompt is read, as a host does with the packs it runs.
/// </summary>
[SetUpFixture]
public class ApiIntegrationPrompts
{
    /// <summary>Registers the made-up pack's guide before any test reads a prompt.</summary>
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([AccountReaderPack.Guides]);
}
