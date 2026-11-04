using LouisAgent.Core.Guardrails.Auth;
using LouisAgent.Core.Config;

namespace LouisAgent.Core.Tests.Guardrails.Auth;

// FX18: a host that serves the agent's tools over the network fails closed. One pure rule, as AdminGate's is, tested
// without a web host: what AGENT_AUTH means, what start-up does with it and the key, and the key check per request.
public class HostAuthTests
{
    [TestCase(null, AuthMode.Required)]
    [TestCase("", AuthMode.Required)]
    [TestCase("   ", AuthMode.Required)]
    [TestCase("required", AuthMode.Required)]
    [TestCase("REQUIRED", AuthMode.Required)]
    [TestCase("off", AuthMode.Off)]
    [TestCase(" Off ", AuthMode.Off)]
    public void TheMode_IsRequiredUnlessOffIsSaid(string? setting, AuthMode expected) =>
        Assert.That(HostAuth.Mode(setting), Is.EqualTo(expected));

    [TestCase("sometimes")]
    [TestCase("1")]
    public void AnUnknownMode_NamesTheSettingAndItsValues(string setting)
    {
        var error = Assert.Throws<ArgumentException>(() => HostAuth.Mode(setting))!;
        Assert.That(error.Message, Does.Contain("AGENT_AUTH").And.Contain(setting).And.Contain("required").And.Contain("off"));
    }

    // Review of the step (2026-10-19): a blank MCP_API_KEY= in an env file is "", which ?? never falls back from, so
    // the server refused to start with AGENT_API_KEY set, or under off started open with a key there. The first
    // non-blank key wins, trimmed, as AdminGate.Key does.
    [TestCase(new[] { "mcp", "agent" }, "mcp")]
    [TestCase(new[] { "", "agent" }, "agent")]
    [TestCase(new[] { "   ", "agent" }, "agent")]
    [TestCase(new[] { null, "agent" }, "agent")]
    [TestCase(new[] { " k " }, "k")]
    [TestCase(new string?[] { "", null }, null)]
    [TestCase(new string?[] { }, null)]
    public void TheKey_IsTheFirstNonBlankOne_Trimmed(string?[] candidates, string? expected) =>
        Assert.That(HostAuth.Key(candidates), Is.EqualTo(expected));

    [Test]
    public void ABlankPreferredKey_FallsBackToTheOther_SoStartupStillChecksIt()
    {
        string? key = HostAuth.Key("", "agent");
        Assert.That(HostAuth.AtStartup(AuthMode.Required, key), Is.EqualTo(HostAuthDecision.Start));
        Assert.That(HostAuth.AtStartup(AuthMode.Off, key), Is.EqualTo(HostAuthDecision.Start), "off never opens a host that has a key");
    }

    [TestCase(AuthMode.Required, null, HostAuthDecision.Refuse)]
    [TestCase(AuthMode.Required, "  ", HostAuthDecision.Refuse)]
    [TestCase(AuthMode.Off, null, HostAuthDecision.StartOpen)]
    [TestCase(AuthMode.Required, "k", HostAuthDecision.Start)]
    // Off never silences a key that is set: a key present is always checked.
    [TestCase(AuthMode.Off, "k", HostAuthDecision.Start)]
    public void AtStartup_RefusesWithoutAKey_UnlessOffIsSaid(AuthMode mode, string? key, HostAuthDecision expected) =>
        Assert.That(HostAuth.AtStartup(mode, key), Is.EqualTo(expected));

    [TestCase("k", null, true)]
    [TestCase(null, "Bearer k", true)]
    [TestCase(null, "bearer k", true)]
    [TestCase("wrong", null, false)]
    [TestCase("", null, false)]
    [TestCase(null, null, false)]
    [TestCase("k2", null, false)]
    [TestCase(null, "Bearer k2", false)]
    // x-api-key wins when both headers are present.
    [TestCase("k", "Bearer wrong", true)]
    [TestCase("wrong", "Bearer k", false)]
    // "Bearer " is a prefix: a bare key in Authorization still passes (the old behaviour, pinned), but the word
    // anywhere else is just part of a wrong key.
    [TestCase(null, "k", true)]
    [TestCase(null, "Basic k", false)]
    [TestCase(null, "kBearer ", false)]
    public void HasKey_ReadsXApiKeyThenBearer_AndComparesWhole(string? apiKeyHeader, string? authorization, bool expected) =>
        Assert.That(HostAuth.HasKey(apiKeyHeader, authorization, expected: "k"), Is.EqualTo(expected));
}
