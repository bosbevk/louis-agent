using LouisAgent.Web.Docs.Contents;

namespace LouisAgent.Web.Framework;

/// <summary>
/// The framework's pages in reading order, under their groups: what the docs layout's menu lists and its page turns
/// step through. A page joins it as it is written (F160.1, steps 6 to 10).
/// </summary>
public static class FrameworkContents
{
    /// <summary>The framework's pages, as they stand.</summary>
    public static readonly DocsContents All = new([
        new DocsGroup("Start", [new DocsEntry("Overview", "/"), new DocsEntry("Getting started", "/getting-started")]),
        new DocsGroup("Understand", [new DocsEntry("How it works", "/how-it-works"), new DocsEntry("Features", "/features")]),
        new DocsGroup("Decide", [
            new DocsEntry("Cost and evals", "/cost-and-evals"), new DocsEntry("Examples", "/examples"),
            new DocsEntry("Comparison", "/comparison")
        ])
    ]);
}
