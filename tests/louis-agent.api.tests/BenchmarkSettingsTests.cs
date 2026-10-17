using LouisAgent.Api.Services;
using LouisAgent.Api.Services.Benchmarks;

namespace LouisAgent.Api.Tests;

// F31 step 6a: a run keeps what it ran with, so a later run is compared with an earlier one knowing what changed.
public class BenchmarkSettingsTests
{
    [Test]
    public void ARun_KeepsTheModelThePersonalitiesAndTheChecksScriptAndArguments()
    {
        var check = new BenchmarkCheck("cook", "Die kokkontrole", "benchmarks/cook/run_check.py", ["--personalities", "puls-cook"]);

        var settings = BenchmarkSettings.Of(check, "anthropic", "claude-haiku-5-5", ["puls/voice-af", "puls-cook"]);

        Assert.That(settings, Is.EquivalentTo(new Dictionary<string, string>
        {
            ["provider"] = "anthropic",
            ["model"] = "claude-haiku-5-5",
            ["personalities_enabled"] = "puls/voice-af,puls-cook",
            ["script"] = "benchmarks/cook/run_check.py",
            ["arguments"] = """["--personalities","puls-cook"]""",
        }));
    }

    [Test]
    public void EveryPersonality_IsOn_WhenNoneAreListed() =>
        Assert.That(BenchmarkSettings.Of(new BenchmarkCheck("c", "C", "s.py", []), "anthropic", "m", [])["personalities_enabled"], Is.EqualTo("(all)"));
}
