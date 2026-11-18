using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Web.Framework.Graphs;

/// <summary>
/// What the coding agent's program loads (F162.1, step 9): the program highlighted, its catalogue's identities and
/// packs, and what the host lends it, each named as the code names it, so a reader can find it.
/// </summary>
/// <remarks>
/// Data for the diagram engine's cards kind, drawn by its own renderer on the page (ADR 0118). The names are those of
/// <c>CodingCatalog</c> and <c>HostServices</c> in the repo, and the page's test holds every identity and pack the
/// catalogue builds to this picture.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public static class WhatItLoads
{
    /// <summary>The picture, as the engine reads it.</summary>
    public static readonly DiagramSource Source = new(DiagramKind.Cards, "What the program loads",
        Cards: new CardsSpec(
        [
            // The page says what the program is, so its card carries no line.
            new CardRow([
                new Card("program", DiagramIcon.Terminal, "The coding agent's program", "Program.cs", "",
                    CardLook.Highlight)
            ]),
            new CardRow([
                new Card("identities", DiagramIcon.User, "Who it can be", "CodingCatalog",
                    "CodingIdentity first, so it is the one you get. Then GuideIdentity and TriageIdentity."),
                new Card("packs", DiagramIcon.Box, "What it can do", "CodingCatalog",
                    "FilesPack, GitPack, DotNetPack, ShellPack, WebPack, DocsPack, TriagePack, LeadsPack and SitePack."),
                new Card("host", DiagramIcon.Server, "What the host lends", "HostServices",
                    "WebAccess, OtlpTelemetry, and the Anthropic, Ollama and OpenAI-compatible providers.")
            ])
        ],
        [new CardLink("program", "identities"), new CardLink("program", "packs"), new CardLink("program", "host")]));
}
