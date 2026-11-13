using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Web.Messages.Holes;

namespace LouisAgent.Web.Messages;

/// <summary>What the framework's web says when a site is set up wrong, at start-up (F144, ADR 0009).</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum WebErrorMessage
{
    [Description(
        "The site names the {Area} area, which plugs into the admin, but no admin frame. Add .Admin(layout) to its " +
        "areas, with the site's admin layout.")]
    [Holes(typeof(AdminAreaWithoutFrameHoles))]
    AdminAreaWithoutFrame,
}
