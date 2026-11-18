using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content;

/// <summary>
/// What How it works tells beside its picture of a turn (F161.1, step 10): the chat pipeline's layers in their real
/// order (<c>ChatPipeline</c>), the web with the backend, every identity and every pack in the repo with a line each
/// from its own description, and the rules every turn keeps with the ADR each comes from.
/// </summary>
/// <remarks>
/// The page's lists as data, so the page only configures the framework's parts (ADR 0125); the page's test holds every
/// identity and pack on the disk to these lists.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public static class HowItWorks
{
    /// <summary>The layers every request to the model goes through, outermost first, as the pipeline builds them.</summary>
    public static readonly IReadOnlyList<Term> Layers =
    [
        new(["the tool loop"], "Runs the model's tool calls, up to 10 rounds a message, each call through the guards."),
        new(["the tools kept"],
            "After the last round, keeps the tool list with tool use off, so the model answers in text and the request " +
            "still matches the ones before it."),
        new(["the usage"], "Records every request in the ledger, each round its own record."),
        new(["the cut-off calls"], "Marks a call the output limit cut short, so it is never run."),
        new(["the trace"], "Traces the request and its answer, with OpenTelemetry."),
        new(["the replay"], "Answers from a recorded run where one is kept, so a replay costs nothing.")
    ];

    /// <summary>How a website and the backend meet.</summary>
    public static readonly IReadOnlyList<Term> TheWeb =
    [
        new(["louis-agent.web"],
            "Every site is built on it, in Blazor WebAssembly, C# running in the browser."),
        new(["the site's host"],
            "Draws each page for a search engine first, then the app takes over in the browser."),
        new(["louis-agent.api"],
            "Holds the sessions, and streams a turn back as server-sent events while it is written.")
    ];

    /// <summary>Every identity in the repo, who it is, in a line.</summary>
    public static readonly IReadOnlyList<Term> Identities =
    [
        new(["louis-agent.identity.coding"], "The coding agent, in Rider and the terminal."),
        new(["louis-agent.identity.triage"], "A team that triages a service's errors."),
        new(["louis-agent.identity.puls"], "Puls, a South African general assistant, with its recipes and shop prices."),
        new(["louis-agent.identity.guide"], "The guide on louis-agent.dev, who answers visitors and shows them round."),
        new(["louis-agent.identity.showroom"], "The agents of the showroom's three made-up businesses.")
    ];

    /// <summary>Every pack in the repo, what it can do, in a line.</summary>
    public static readonly IReadOnlyList<Term> Packs =
    [
        new(["louis-agent.pack.files"], "The workspace's files. Read, write, search, and PDF, Word and Excel as text."),
        new(["louis-agent.pack.git"], "Git. Status, diffs, branches, commits, merges and history."),
        new(["louis-agent.pack.dotnet"], "The .NET SDK. Build, test, run and restore projects."),
        new(["louis-agent.pack.shell"], "Python, PowerShell and bash scripts."),
        new(["louis-agent.pack.web"], "The web. Search, read a page, check a quote, convert the money it names."),
        new(["louis-agent.pack.docs"], "A project's own docs. List, read and search its markdown."),
        new(["louis-agent.pack.calculator"], "Sums worked out in code, so a model doesn't do them in its head."),
        new(["louis-agent.pack.devops"], "Azure DevOps. Work items, sprints and team queries."),
        new(["louis-agent.pack.paymo"], "Paymo time tracking. Time entries, tasks, projects and summaries."),
        new(["louis-agent.pack.leads"], "Tells a site's owner that someone is interested."),
        new(["louis-agent.pack.site"], "Takes a visitor to a page of the site, and to a place on it."),
        new(["louis-agent.pack.triage"], "What a team triages a service's errors with, starting from the error feed."),
        new(["louis-agent.pack.recipes"], "A recipe, read from a page's own schema.org data."),
        new(["louis-agent.pack.shop-prices"], "A shopping list's cost, estimated from Stats SA's average prices."),
        new(["louis-agent.pack.where-to-buy"], "A shop's branches near a town, from OpenStreetMap.")
    ];

    /// <summary>The rules every turn keeps, each held in code.</summary>
    public static readonly IReadOnlyList<Term> Rules =
    [
        new(["arguments as data"], "A tool takes its arguments as data, never through a shell."),
        new(["guard the context"],
            "A result over 50,000 characters is summarised, a cut-off call is never run, and a failure carries its reason."),
        new(["history is append-only"],
            "Nothing edits the system prompt, the tools or an earlier message, and the turn is kept whole."),
        new(["measure every request"], "Every request is priced in the ledger, and unknown is null, never nought."),
        new(["take a site's no"], "Every fetch obeys robots.txt and a page's no-AI signals, and says what it is.")
    ];
}
