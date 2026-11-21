namespace LouisAgent.Choices;

/// <summary>
/// The one way an enum member becomes text: <c>mode.Wire()</c> (F18 step 7.5.2, Louis: "the system should work like this everywhere"). In with <see cref="SettingChoice.TryParse{T}"/>, out with
/// <c>.Wire()</c>, the text on <see cref="WireNameAttribute"/>. Outside this project no code calls <see cref="SettingChoice.Wire{T}"/> itself, and no enum is turned into text with <c>ToString()</c>,
/// <c>ToLowerInvariant()</c> or a naming policy; a ratchet holds it.
/// </summary>
public static class WireExtensions
{
    /// <summary>The text the member travels as: its <see cref="WireNameAttribute"/>, else its own name in lower case; a value the enum does not define is refused.</summary>
    public static string Wire<T>(this T member) where T : struct, Enum => SettingChoice.Wire(member);
}
