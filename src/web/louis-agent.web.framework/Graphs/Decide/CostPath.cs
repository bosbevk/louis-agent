using LouisAgent.Diagrams.Sources;
using LouisAgent.Web.Framework.Content.Cost;

namespace LouisAgent.Web.Framework.Graphs.Decide;

/// <summary>
/// A request's path to rand (F162.1, step 12, Louis's pick A of three): the request, its tokens, the dated price
/// table, the cost in dollars, the ledger that keeps it, and the rand a page shows, each with the worked request's own
/// numbers. Three along the top and three under them running back, with room between the rows, so it fits the page's
/// width (Louis: "can we move the boxes like this to make it fit?", "make it 3 boxes top 3 bottom and increase the
/// space between top and bottom a bit").
/// </summary>
/// <remarks>
/// A flow for the diagram engine, written as the JSON its reader takes, drawn by its own renderer on the page (ADR
/// 0118). Its numbers come from <see cref="CostAndEvals.Example"/>, which the page's test reads from the real price
/// table and rates, so the picture and the steps under it can't disagree. Chosen over the cost at a glance, which
/// repeats the sections under it, and over who prices it and when, which overlaps How it works' turn.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public static class CostPath
{
    /// <summary>The room between the top row and the row under it.</summary>
    private const double RowGap = 56;

    /// <summary>The picture, as the engine reads it.</summary>
    public static readonly DiagramSource Source = Build(CostAndEvals.Example);

    /// <summary>Builds the path for a worked request, its numbers in its nodes.</summary>
    private static DiagramSource Build(CostExample example) => DiagramReader.Read($$"""
        {
          "kind": "flow",
          "type": "activity",
          "title": "A request's path to rand",
          "flow": {
            "direction": "right",
            "rowGap": {{RowGap}},
            "nodes": [
              { "id": "request", "label": "One request\n{{example.Model}}", "icon": "message", "tone": "blue",
                "shape": "terminal", "column": 0 },
              { "id": "tokens", "label": "Its tokens\n{{example.InputTokens}} in, {{example.OutputTokens}} out",
                "icon": "chip", "tone": "violet", "column": 1 },
              { "id": "prices", "label": "prices.json\n{{example.PricesAsOf}}", "icon": "file", "tone": "amber",
                "shape": "data", "column": 2 },
              { "id": "cost", "label": "The cost\n{{example.DollarsText}}", "icon": "chart", "tone": "violet",
                "column": 2 },
              { "id": "ledger", "label": "The ledger\nkept in dollars", "icon": "database", "tone": "amber",
                "shape": "data", "column": 1 },
              { "id": "rand", "label": "Shown in rand\n{{example.RandText}}", "icon": "browser", "tone": "violet",
                "shape": "terminal", "column": 0 }
            ],
            "edges": [
              { "from": "request", "to": "tokens" },
              { "from": "tokens", "to": "prices", "word": "a million at a time" },
              { "from": "prices", "to": "cost" },
              { "from": "cost", "to": "ledger" },
              { "from": "ledger", "to": "rand", "word": "R{{example.RandPerDollar}} a dollar" }
            ]
          }
        }
        """);
}
