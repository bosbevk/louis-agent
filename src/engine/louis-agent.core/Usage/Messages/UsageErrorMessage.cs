
using System.ComponentModel;

namespace LouisAgent.Core.Usage.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the usage ledger area: what is wrong and how to fix it.</summary>
public enum UsageErrorMessage
{
    [Description("{Path} line {Number} is not a usage record: {Reason}")]
    NotAUsageRecord,
}
