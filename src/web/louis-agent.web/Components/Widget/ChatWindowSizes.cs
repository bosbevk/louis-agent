namespace LouisAgent.Web.Components.Widget;

/// <summary>
/// What a chat window's size means to its styles (F134.3): the class a window wears at each size, asked by every
/// chat that has the window's buttons.
/// </summary>
/// <remarks>
/// What is known about a size lives on its type (ADR 0123). The guide's widget and a mock website's chat each wrote
/// the same switch, so they agreed only while the copies did.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public static class ChatWindowSizes
{
    /// <summary>The window's classes at this size: its own, and the size's when it isn't the normal one.</summary>
    public static string ClassesOf(this ChatWindowSize size, string window) => size switch
    {
        ChatWindowSize.Minimised => $"{window} is-min",
        ChatWindowSize.Maximised => $"{window} is-max",
        _ => window,
    };
}
