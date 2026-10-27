using System.ComponentModel;

namespace LouisAgent.Api.Messages;

/// <summary>
/// What the notes route says when it turns a note down.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text lives in enum templates</seealso>
public enum NoteUserText
{
    [Description("A note needs some text.")]
    NoteBlank,

    [Description("A note is at most {Limit} characters; this one has {Length}.")]
    NoteTooLong,
}
