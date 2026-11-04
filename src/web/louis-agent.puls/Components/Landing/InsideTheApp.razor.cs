using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Landing;

public partial class InsideTheApp
{
    // The pinned tour's chips, in the order of its panels.
    private static readonly (string Icon, string Label, string Title, string Text)[] Tour =
    [
        ("search", "Soek", "Soek aanlyn", "Die spesialis soek, lees die bladsye self en gee die bronne by die antwoord."),
        ("file", "Lees", "Lees lêers", "Heg 'n PDF, 'n Word-dokument of 'n sigblad aan; die spesialis lees dit en som dit op."),
        ("clock", "Geskiedenis", "Kyk na die geskiedenis", "Wat het verander, wie het dit gedoen en waarom: uit die repository se geskiedenis."),
        ("coin", "Koste", "Koste sigbaar", "Elke vraag se koste, spesialiste ingesluit, en jou gebruik vir die maand, in rand."),
    ];
}
