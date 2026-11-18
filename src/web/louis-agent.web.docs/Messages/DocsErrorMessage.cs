using System.ComponentModel;

namespace LouisAgent.Web.Docs.Messages;

/// <summary>What the docs' parts say when a site is set up wrong (F162.1, ADR 0009).</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum DocsErrorMessage
{
    [Description(
        "A page links to a doc, but the site names no docs' address. Add .Docs(DocsAddress.GitHub(repository, " +
        "branch)) to its areas, or .Docs(new DocsAddress(site)) once the docs have a site of their own.")]
    NoDocsAddress
}
