using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Editing.Inline;

/// <summary>
/// How a component that draws a part of a value lets that part be changed in place (F142): it listens for its part
/// exactly when someone listens for the whole, and tells a changed part as the whole with that part changed.
/// </summary>
/// <remarks>
/// State goes down as parameters and comes back up as callbacks, as everywhere in the web. This is the one rule
/// that makes the same components a public page and its editor: where nobody listens for the whole, no part has a
/// listener, and <see cref="InlineEdit"/> draws plain words. Without it an "editing" flag would be passed down
/// beside every part, and one forgotten would leave a public page with something to click.
/// </remarks>
public static class PartChanges
{
    /// <summary>A listener for a part, which tells the whole's listener the whole with that part changed.</summary>
    /// <param name="whole">Who listens for the whole, if anyone.</param>
    /// <param name="receiver">The component drawing the part, which is redrawn after a change.</param>
    /// <param name="changed">The whole with the part changed.</param>
    public static EventCallback<TPart> Of<TWhole, TPart>(this EventCallback<TWhole> whole, object receiver,
        Func<TPart, TWhole> changed) =>
        whole.HasDelegate
            ? EventCallback.Factory.Create<TPart>(receiver, part => whole.InvokeAsync(changed(part)))
            : default;
}
