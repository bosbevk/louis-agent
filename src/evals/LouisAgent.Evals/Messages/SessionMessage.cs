using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages.Holes.Sessions;

namespace LouisAgent.Evals.Messages;

/// <summary>
/// What an eval says when the sessions API, or the site's page route, won't do what it asked (ADR 0009: a text is a
/// member of a message enum, its <c>[Description]</c> a template with named holes).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum SessionMessage
{
    [Description("The sessions API refused, with {Status}: {Reason}")]
    [Holes(typeof(RefusedHoles))]
    Refused,

    /// <summary>F126.3.3: the page a conversation names is not a page of the site, so the run can't send it.</summary>
    [Description("The page {Address} is not a page of this site, so it can't go with the conversation's first ask.")]
    [Holes(typeof(PageMissingHoles))]
    PageMissing,

    /// <summary>F126.3.3: a replay found no kept page for its trial, so its recording is out of date.</summary>
    [Description("The recording holds no page at {Address} for this conversation.")]
    [Holes(typeof(PageNotRecordedHoles))]
    PageNotRecorded
}
