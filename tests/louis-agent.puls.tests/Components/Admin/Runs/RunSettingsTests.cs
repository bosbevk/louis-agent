using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Components.Admin.Runs;

namespace LouisAgent.Puls.Tests.Components.Admin.Runs;

/// <summary>
/// What a run ran with, pinned to <c>RunSettings.full.html</c> (the eval and graders, the settings, two modes, the
/// runner's lines) and <c>RunSettings.bare.html</c> (a run that kept no settings, graders or lines).
/// </summary>
public class RunSettingsTests
{
    /// <summary>A run with everything it ran with.</summary>
    private static readonly string Full = SampleMode.Fill(SampleToolset.Fill(
        """
        {"run":{"id":"run_1","check":"check-x","repeat":1,"status":"done",
                "started_at":"2026-10-27T10:00:00+00:00","eval":"stub","eval_version":2,"commit":"abc1234",
                "graders":[{"name":"no-error","version":1,"kind":"fixed-rule","question":null}]},
         "settings":{"model":"claude-haiku-5-5","web":"record"},
         "modes":[{"name":"<ModeAlpha>","repeat":1,"toolsets":["<Alpha>","<Beta>"],"role":"puls/voice-af",
                   "llm":"haiku"},
                  {"name":"<ModeBeta>","repeat":1,"toolsets":["<Gamma>"],"role":null,"llm":null}],
         "log":["started","done"],
         "notes":[],"conversations":[],"agreement":[],"turns":[],"requests":[]}
        """));

    /// <summary>A run that kept no settings, graders or lines.</summary>
    private static readonly string Bare = SampleMode.Fill(
        """
        {"run":{"id":"run_0","check":"check-x","repeat":1,"status":"done",
                "started_at":"2026-10-27T10:00:00+00:00"},
         "settings":null,"modes":[{"name":"<ModeAlpha>","repeat":1,"toolsets":[],"role":null,"llm":null}],
         "log":null,"notes":[],"conversations":[],"agreement":[],"turns":[],"requests":[]}
        """);

    /// <summary>The run as the component takes it: the contract's reply.</summary>
    private static StoredRunReply RunOf(string json) =>
        JsonSerializer.Deserialize<StoredRunReply>(json, AdminJson.SnakeCase)!;

    /// <summary>Everything the run ran with shows as before.</summary>
    [Test]
    public void AFullRun_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<RunSettings>(p => p.Add(c => c.Run, RunOf(Full)));

        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/Runs/RunSettings.full.html", cut.Markup);
    }

    /// <summary>A run that kept little shows only its mode.</summary>
    [Test]
    public void ABareRun_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<RunSettings>(p => p.Add(c => c.Run, RunOf(Bare)));

        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/Runs/RunSettings.bare.html", cut.Markup);
    }
}
