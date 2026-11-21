using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Landing;

public partial class Features
{
    // The bento's tiles: the area letter places each one in the grid ("a a b" / "c d b" / "c e f"), and the three with
    // an extra (pulse, search, coin) get it from the switch in the markup.
    private static readonly (char Area, string Icon, string Title, string Text)[] Tiles =
    [
        ('a', "pulse", "Sien hoe dit werk", "Elke taak se stappe, gedagtes en gereedskap verskyn lewendig terwyl die spesialis werk."),
        ('b', "search", "Soek aanlyn", "Die spesialis soek op die web en lees die bladsye self, met die bronne by die antwoord."),
        ('c', "coin", "Koste sigbaar", "Elke vraag wys wat dit gekos het, in jou eie geldeenheid."),
        ('d', "file", "Lees lêers", "Vra oor 'n lêer of heg een aan: Puls lees dit en som dit op."),
        ('e', "clock", "Kyk na die geskiedenis", "Wat het verander, wie het dit gedoen en waarom: uit die repository se geskiedenis."),
        ('f', "chart", "Tel en reken", "Laat die spesialis tel, reken en tabelle maak met 'n skrip wat jy kan nagaan."),
    ];

    // What rolls past under the bento: the toolsets (set) and their tools (tool).
    private static readonly (string Kind, string Name)[] Marquee =
    [
        ("set", "web"), ("tool", "WebSearch"), ("tool", "FetchUrl"), ("set", "files"), ("tool", "ReadFile"), ("tool", "ReadDocument"),
        ("tool", "ReadTable"), ("set", "git"), ("tool", "GitLog"), ("tool", "GitShow"), ("set", "dotnet"), ("tool", "DotNetTest"),
        ("set", "python"), ("tool", "PythonRun"), ("set", "clock"), ("tool", "GetToday"), ("tool", "ConvertCurrency"),
    ];
}
