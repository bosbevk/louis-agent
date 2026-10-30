using System.Text.Json;

namespace LouisAgent.Evals.Files;

/// <summary>
/// An eval as its <c>eval.json</c> says it: what it is called, whose it is, its version, what it proves, the modes
/// it runs in and the checks it grades with. Nothing here runs; it is the file's shape.
/// </summary>
/// <remarks>
/// A Data Transfer Object (Fowler, PoEAA) read through the serializer, never picked apart by hand. The checks are
/// named by their catalogue type, so the file holds no code. The version is a whole number bumped by hand when a
/// scenario, a mode or a check changes, as today.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed record EvalFile(
    int SchemaVersion,
    string Name,
    string Identity,
    int Version,
    string Proves,
    IReadOnlyList<EvalModeFile> Modes,
    IReadOnlyList<CheckEntryFile> Checks,
    string? Manual = null);

/// <summary>One mode the eval runs in: its name, the role a session opens with, and the toolsets it names.</summary>
public sealed record EvalModeFile(string Name, string Role, IReadOnlyList<string> Toolsets);

/// <summary>
/// One check the eval grades with: its catalogue type, a name only where a stored run already carries one that
/// differs from the type (none of Puls's today), and its settings, left out where the entry's defaults are enough.
/// </summary>
public sealed record CheckEntryFile(string Type, string? Name = null, JsonElement? Parameters = null);
