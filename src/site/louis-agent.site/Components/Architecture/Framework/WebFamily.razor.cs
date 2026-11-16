using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Site.Components.Architecture.Framework;

/// <summary>The framework's web on the Architecture page: the libraries every site is built on.</summary>
public partial class WebFamily
{
    /// <summary>Each library of the web, and what it is.</summary>
    private static readonly IReadOnlyList<Term> Terms =
    [
        new(["louis-agent.web"], "The chat's and the admin's frames, the site frame this page sits in, the controls a site configures, the chat widget in the corner, the described screens, the API client, markdown, usage and currency."),
        new(["louis-agent.web.accounts"], "The account pages, their client, words, styles and passkey script. A site with accounts opts in."),
        new(["louis-agent.web.evals"], "The eval studio's pages: the evals, a run, the review queue, the studio and the prompts."),
        new(["louis-agent.web.themes"], "The looks a site can offer, glass and flat, each in a light and a dark style. A site names the ones it wants."),
        new(["louis-agent.diagrams"], "What a diagram is, as data in plain C#: its kinds, its records and the reader that checks a picture's file."),
        new(["louis-agent.diagrams.engine"], "What draws one: the layouts, the drawings and the looks behind the boxes and arrows on these pages, and the pictures in the framework's docs.")
    ];
}
