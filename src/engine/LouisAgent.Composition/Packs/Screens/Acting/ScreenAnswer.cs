using LouisAgent.Composition.Messages;

namespace LouisAgent.Composition.Packs.Screens.Acting;

/// <summary>What came of an action or a save: whether it happened, what to tell the person, and any field it refused.</summary>
/// <param name="Done">Whether it happened.</param>
/// <param name="Message">What to tell the person, in the pack's words.</param>
/// <param name="FieldErrors">A form's refused fields, by name, each with why.</param>
public sealed record ScreenAnswer(bool Done, string Message, IReadOnlyDictionary<string, string> FieldErrors)
{
    /// <summary>It happened.</summary>
    public static ScreenAnswer Ok(string message) => new(true, message, new Dictionary<string, string>());

    /// <summary>It didn't happen, and here's why.</summary>
    public static ScreenAnswer No(string message) => new(false, message, new Dictionary<string, string>());

    /// <summary>What a screen answers for something it never offered: read-only is the default.</summary>
    public static ScreenAnswer NotOffered { get; } = No(MessageText.Format(ScreenMessage.NotOffered));
}
