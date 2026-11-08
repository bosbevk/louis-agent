using LouisAgent.Web.Graphs;

namespace LouisAgent.CodeReview.Graphs.Framework;

/// <summary>
/// The How-it-works page's second diagram (F119): the web working with the backend. A visitor's browser opens the
/// site on the framework's web, whose guide in the corner sends a message with the page on screen; the API, at the
/// same address, checks the sign-in and runs a turn on the engine; the engine asks the model and may call the site
/// pack's ShowPage, which moves the page back in the browser. <c>WebGraphTests</c> holds the boxes to the parts.
/// </summary>
public static class WebGraph
{
    /// <summary>The boxes and arrows.</summary>
    public static GraphSpec Spec { get; } = new(
        [
            new GraphNode("browser", "A visitor's browser", null, 0),
            new GraphNode("site", "The site", "the framework's web", 1),
            new GraphNode("widget", "The guide's widget", "reads the page, moves it", 1),
            new GraphNode("api", "The API", "the same address", 2),
            new GraphNode("accounts", "Accounts", "a sign-in, a tenant", 2),
            new GraphNode("engine", "The engine", "the loop, the guards, the ledger", 3),
            new GraphNode("model", "The model", null, 4),
            new GraphNode("show", "ShowPage", "the site pack's tool", 4),
        ],
        [
            new GraphEdge("browser", "site", "opens"),
            new GraphEdge("site", "api", "a session"),
            new GraphEdge("widget", "api", "a message, the page"),
            new GraphEdge("api", "accounts"),
            new GraphEdge("api", "engine", "a turn"),
            new GraphEdge("engine", "model"),
            new GraphEdge("engine", "show", "calls"),
            new GraphEdge("show", "widget", "moves the page"),
        ]);
}
