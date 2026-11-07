using LouisAgent.Api.Config;

namespace LouisAgent.Api.Tests.Config;

/// <summary>
/// The admin portal's one setting, its key, read the way the agent's own options are: a blank value counts as no
/// value, and the value is trimmed. The studio's settings are its own (EvalOptionsTests, CL57).
/// </summary>
public class AdminOptionsTests
{
    /// <summary>Reads the options from just the variables you give it, as if nothing else were set.</summary>
    private static AdminOptions From(params (string Name, string Value)[] variables)
    {
        var map = variables.ToDictionary(v => v.Name, v => v.Value);
        return AdminOptions.FromEnvironment(name => map.GetValueOrDefault(name));
    }

    /// <summary>With nothing set the portal stays shut.</summary>
    [Test]
    public void WithNothingSet_ThePortalIsClosed()
    {
        var options = From();

        Assert.That(options.AdminKey, Is.Null, "no key: the portal stays closed (F27)");
    }

    /// <summary>The admin key is its own when set, and falls back to the agent's key when it's blank.</summary>
    [Test]
    public void TheAdminKey_IsAdminApiKey_ElseTheAgentsKey()
    {
        Assert.That(From(("ADMIN_API_KEY", " admin "), ("AGENT_API_KEY", "agent")).AdminKey, Is.EqualTo("admin"));
        Assert.That(From(("ADMIN_API_KEY", "  "), ("AGENT_API_KEY", " agent ")).AdminKey, Is.EqualTo("agent"));
    }
}
