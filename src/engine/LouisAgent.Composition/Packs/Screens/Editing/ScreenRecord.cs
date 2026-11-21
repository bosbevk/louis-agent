using LouisAgent.Choices;

namespace LouisAgent.Composition.Packs.Screens.Editing;

/// <summary>One row as a form: its heading, and its fields in the order they read.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public sealed record ScreenRecord(string Title, IReadOnlyList<ScreenField> Fields);

/// <summary>One field of a form. The pack checks what's saved, and a refusal names the field.</summary>
/// <param name="Name">What the save calls it.</param>
/// <param name="Label">What it says on the form.</param>
/// <param name="Kind">How it's edited.</param>
/// <param name="Value">What it holds now; null for what isn't known, and for a field of rows.</param>
/// <param name="ReadOnly">Shown, but it can't be changed.</param>
/// <param name="Required">It can't be left empty.</param>
public sealed record ScreenField(
    string Name,
    string Label,
    ScreenFieldKind Kind,
    string? Value = null,
    bool ReadOnly = false,
    bool Required = false)
{
    /// <summary>A field of rows: the heading of each of a row's cells.</summary>
    public IReadOnlyList<string> RowColumns { get; init; } = [];

    /// <summary>A field of rows: its rows, in order.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];
}

/// <summary>The whole vocabulary for a field. A new one enters only when a second screen needs it.</summary>
public enum ScreenFieldKind
{
    /// <summary>Plain words.</summary>
    [WireName("text")] Text,

    /// <summary>A number.</summary>
    [WireName("number")] Number,

    /// <summary>A web address.</summary>
    [WireName("link")] Link,

    /// <summary>A list, a row per line, each with the same cells.</summary>
    [WireName("rows")] Rows
}

/// <summary>What a form sends back when it's saved.</summary>
/// <param name="Values">The plain fields, by name.</param>
/// <param name="Rows">The fields of rows, by name, in the order shown.</param>
public sealed record ScreenValues(
    IReadOnlyDictionary<string, string?> Values,
    IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>> Rows);
