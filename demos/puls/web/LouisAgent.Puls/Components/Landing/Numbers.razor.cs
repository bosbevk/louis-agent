using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Landing;

public partial class Numbers
{
    // The measured numbers, from the learning write-ups (F5's toolset profiles, the puls check, the demo's prompt
    // cache) — the counter's target, its format (pct, int or rand), its text before counting, a unit after it, what it
    // is and how it was measured.
    private static readonly (string To, string Fmt, string Start, string? Unit, string What, string How)[] Stats =
    [
        ("50", "pct", "−0%", null, "kleiner prompt per gesprek", "~48,5k → ~24,4k tokens met toolset-profiele"),
        ("24", "int", "0", "/24", "take by die regte spesialis", "die afvaardigingstoets, drie lopies"),
        ("10", "int", "0", "/10", "regstellings in die demo bevestig", "met prompt-kas: $0,66 → $0,13 per lopie"),
        ("0.02", "rand", "R0,00", null, "om 'n README op te som", "spesialis ingesluit, in rand"),
    ];
}
