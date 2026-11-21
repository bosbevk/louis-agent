using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Web.Framework.Graphs;

/// <summary>
/// The framework at a glance, the Overview's opening picture (F164.1, Louis, of the hero: "do one like this highlight
/// the important ones"): louis-agent highlighted, joined to the three ideas everything in it is one of, the engine, an
/// identity and a pack, with where it runs under them.
/// </summary>
/// <remarks>
/// Data for the diagram engine's cards kind, the look the projects' docs open with, drawn by the engine's own renderer
/// on the page (ADR 0118): the page holds no hero of its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public static class FrameworkAtAGlance
{
    /// <summary>The picture, as the engine reads it.</summary>
    public static readonly DiagramSource Source = new(DiagramKind.Cards, "louis-agent at a glance",
        Cards: new CardsSpec(
        [
            new CardRow([
                // The page's heading says what the framework is, so the picture's top card carries no line.
                new Card("framework", DiagramIcon.Stack, "The C# AI agent framework", "louis-agent", "",
                    CardLook.Highlight)
            ]),
            new CardRow([
                new Card("engine", DiagramIcon.Chip, "The engine", null,
                    "How it runs. The loop, its guards, the model providers and the usage ledger."),
                new Card("identity", DiagramIcon.User, "An identity", null,
                    "Who the agent is. Its voice, its rules and its skills, as markdown you can read."),
                new Card("pack", DiagramIcon.Box, "A pack", null,
                    "What it can do. Tools in a package of their own, which an MCP server can serve.")
            ]),
            new CardRow([
                new Card("hosts", DiagramIcon.Server, "Where people meet it", null,
                    "The terminal, Rider, a website, an HTTP API or an MCP server."),
                new Card("hardware", DiagramIcon.Home, "On your own hardware", null,
                    "Docker on your machine or server, a local model through Ollama if you want.")
            ])
        ],
        [
            new CardLink("framework", "engine"),
            new CardLink("framework", "identity"),
            new CardLink("framework", "pack")
        ]));
}
