using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Screens.Forms;

/// <summary>
/// A list you edit a row per line, such as a recipe's ingredients with their sections. It changes the list it's given,
/// so the order shown is the order the form sends.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public partial class ScreenRowsEditor
{
    /// <summary>The heading of each cell in a row.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<string> Columns { get; set; } = [];

    /// <summary>The rows, in order, which this changes.</summary>
    [Parameter, EditorRequired] public List<List<string>> Rows { get; set; } = [];

    /// <summary>Puts an empty row at the end.</summary>
    private void Add() => Rows.Add([.. Columns.Select(_ => "")]);

    /// <summary>Takes this row out.</summary>
    private void Remove(int at) => Rows.RemoveAt(at);

    /// <summary>Swaps this row with the one that many places along.</summary>
    private void Move(int at, int by) => (Rows[at], Rows[at + by]) = (Rows[at + by], Rows[at]);
}
