using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.SiteApi.Messages.Holes;

namespace LouisAgent.SiteApi.Messages;

/// <summary>What the site's host answers the admin's CV editor when a save can't be kept (F142).</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum SiteApiUserText
{
    [Description("The CV can't be read as JSON Resume: {Reason}")]
    [Holes(typeof(CvNotReadHoles))]
    CvNotRead,

    [Description("The CV can't be saved: SITE_DATA_DIRECTORY isn't set, so there is nowhere to keep it.")]
    CvNowhereToKeep,

    [Description("The CV holds an ID number, which a public page must not show. Take it out and save again.")]
    CvHoldsIdNumber,

    [Description("The CV holds a salary, which a public page must not show. Take it out and save again.")]
    CvHoldsSalary,

    [Description("The CV holds a birth date, which a public page must not show. Take it out and save again.")]
    CvHoldsBirthDate,

    [Description("The CV holds a street address, which a public page must not show. Take it out and save again.")]
    CvHoldsAddress,
}
