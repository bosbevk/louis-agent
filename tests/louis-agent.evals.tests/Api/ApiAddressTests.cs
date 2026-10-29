using LouisAgent.Evals.Api;

namespace LouisAgent.Evals.Tests.Api;

/// <summary>
/// The runner finds the agent API at AGENT_API, and still at PULS_API, its old name, so a shell or an older API that
/// sets only the old one keeps working until a TODO line removes it.
/// </summary>
public class ApiAddressTests
{
    /// <summary>Reads the address from just the variables you give it, as if nothing else were set.</summary>
    private static string? From(params (string Name, string Value)[] variables)
    {
        var map = variables.ToDictionary(v => v.Name, v => v.Value);
        return HttpSessionClient.ApiFrom(name => map.GetValueOrDefault(name));
    }

    /// <summary>The new name is read.</summary>
    [Test]
    public void TheAddress_IsAgentApi() =>
        Assert.That(From(("AGENT_API", "http://agent:8080/")), Is.EqualTo("http://agent:8080/"));

    /// <summary>The old name still works when it's the only one set.</summary>
    [Test]
    public void TheOldName_IsStillRead() =>
        Assert.That(From(("PULS_API", "http://old:8080/")), Is.EqualTo("http://old:8080/"));

    /// <summary>With both set the new name wins, and a blank new one counts as not set.</summary>
    [Test]
    public void TheNewName_Wins_UnlessItIsBlank()
    {
        Assert.That(From(("AGENT_API", "http://new/"), ("PULS_API", "http://old/")), Is.EqualTo("http://new/"));
        Assert.That(From(("AGENT_API", "  "), ("PULS_API", "http://old/")), Is.EqualTo("http://old/"));
    }

    /// <summary>With neither set there's no address, and the client falls back to its default.</summary>
    [Test]
    public void WithNeither_ThereIsNoAddress() => Assert.That(From(), Is.Null);
}
