
using System.ComponentModel;

namespace LouisAgent.Core.Tools.Clock.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the clock toolset, returned to the model: what went wrong and what to do instead.</summary>
public enum ClockToolMessage
{
    [Description("{Weekday} {Date}, {Time} in {Zone} (UTC{Offset}). ISO: {Iso}. Week {Week}.")]
    Today,
}
