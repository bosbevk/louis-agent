using LouisAgent.Core.Skills;

/// <summary>
/// The API's tests open the test agent's sessions (F166.1), so they register its prompts and its packs' (the fakes
/// under the web's and the calculator's names) before the first prompt is read, as a host does with the identities it
/// runs; and the test business another agent asks in its voice (<c>TestBusiness</c>, F127.3.1).
/// </summary>
[SetUpFixture]
public class ApiTestPrompts
{
    /// <summary>Registers the test agent's prompts and the packs' before any test reads one.</summary>
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use(
    [
        LouisAgent.Api.Testing.TestAgent.Identity.Prompts,
        TestTools.WebPack.Prompts,
        TestTools.Pack.Prompts,
        TestTools.CalculatorPack.Prompts,
        LouisAgent.Api.Tests.TestBusiness.Identity.Prompts,
    ]);
}
