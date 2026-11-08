using System.ComponentModel;

namespace LouisAgent.Packs.Docs.Messages;

/// <summary>The docs pack's texts the model reads (ADR 0009), pinned by <c>DocsToolMessageSnapshot.json</c>.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum DocsToolMessage
{
    [Description("There is no doc at '{Path}'. ListDocs shows what is there.")]
    DocNotFound,

    [Description("There is no folder '{Path}' in the docs. ListDocs with no folder shows the top.")]
    FolderNotFound,

    [Description("'{Path}' is outside the docs: only a path under the docs root, with no '..', can be read.")]
    OutsideTheDocs,

    [Description("'{Path}' is not a doc: only .md and .txt files are read here.")]
    NotADoc,

    [Description("'{Path}' is over 1 MB; a doc that size is not read whole. Search it instead.")]
    DocTooLarge,

    [Description("SearchDocs needs some text to look for.")]
    QueryEmpty,

    [Description("Nothing in the docs contains '{Query}'. Try another word, or ListDocs to see what is there.")]
    NothingFound,

    [Description("'{Path}' is empty.")] FolderEmpty,
}
