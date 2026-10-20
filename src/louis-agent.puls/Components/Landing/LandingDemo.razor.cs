using Microsoft.AspNetCore.Components;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LouisAgent.Puls.Components.Landing;

public partial class LandingDemo
{
    /// <summary>One scripted turn: the sidebar's title, the question, the specialist's toolsets, its steps (tool, arguments), the answer (a little HTML), its sources, and what it cost.</summary>
    private sealed record Turn(string Title, string Question, string[] Toolsets, string[][] Steps, string Answer, string[] Sources, decimal Cost, int Requests);

    private static readonly Turn[] Turns =
    [
        new("Wat is nuut in .NET 10?", "Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel?", ["web"],
            [["GetToday", ""], ["WebSearch", "\".NET 10 release\""], ["FetchUrl", "learn.microsoft.com"], ["FetchUrl", "github.com/dotnet/core"]],
            "Die nuutste <b>stabiele</b> weergawe is <b>.NET 10.0.13</b>, ’n LTS-vrystelling van 13 Oktober 2026 (ondersteun tot November 2028). .NET 11 is nog ’n release candidate, nie vir produksie nie.",
            ["learn.microsoft.com", "github.com/dotnet/core"], 0.06m, 4),
        new("Som die README op", "Som die README op in drie punte.", ["files"],
            [["ReadFile", "README.md"]],
            "<b>1.</b> Louis Agent is ’n selfgehoste KI-kodeagent in .NET wat met regte gereedskap op jou repository werk. <b>2.</b> Jy praat daarmee deur Rider, ’n webapp, ’n API of die terminal. <b>3.</b> Claude is die verstek; Ollama en OpenAI-versoenbare modelle werk ook.",
            ["README.md"], 0.02m, 2),
        new("Laaste drie commits", "Wat het die laaste drie commits verander?", ["git"],
            [["GitLog", "-3"], ["GitShow", "d577822"], ["GitShow", "36e63d8"], ["GitShow", "9b600ca"]],
            "<b>FX15</b> laat ’n toets die tariewe-lêer self noem, <b>F5</b> rol ’n spesialis se gebruik op in die ouergesprek se koste, en <b>FX9</b> skryf ’n gekanselleerde versoek na die grootboek. Al drie van 14 Oktober.",
            ["git log", "docs/fixes"], 0.03m, 3),
    ];

    // The shape js/landing.js plays: q, sets, steps, a, src, cost, req, chat. Relaxed escaping keeps the Afrikaans
    // apostrophes and the answer's <b> tags readable; Razor encodes the attribute value itself.
    private static readonly string TurnsJson = JsonSerializer.Serialize(
        Turns.Select((t, i) => new { q = t.Question, sets = t.Toolsets, steps = t.Steps, a = t.Answer, src = t.Sources, cost = t.Cost, req = t.Requests, chat = i }),
        new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
}
