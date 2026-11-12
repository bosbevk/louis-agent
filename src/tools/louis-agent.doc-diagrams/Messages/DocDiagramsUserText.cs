using System.ComponentModel;

namespace LouisAgent.DocDiagrams.Messages;

/// <summary>What the docs' diagram tool says (ADR 0009: a text is a member of a message enum).</summary>
public enum DocDiagramsUserText
{
    [Description("{Path} written.")] Written,

    [Description("{Path} is not what its JSON draws: run the tool and commit what it writes.")]
    Stale,

    [Description("{Path} can't be drawn: {Reason}")]
    Unreadable,

    [Description("{Written} written, {Unchanged} unchanged, {Stale} stale, {Failed} that can't be drawn.")]
    Summary,

    [Description("This Mermaid line can't be read: {Line}")]
    UnreadMermaid,

    [Description("{Path} written from lines {Start} to {End} of {Doc}. Put this in their place: {Link}")]
    Imported
}
