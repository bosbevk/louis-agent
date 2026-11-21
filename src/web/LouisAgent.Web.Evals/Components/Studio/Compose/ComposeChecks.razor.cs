using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Web.Evals.Admin.Studio.Compose;
using LouisAgent.Web.Evals.Admin.Studio.Schema;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Studio.Compose;

/// <summary>
/// An eval's checks on the compose page (F49.5): each picked from the runner's catalogue, its fields drawn from its
/// settings' schema by <see cref="SchemaFields"/>, a reader picked from the catalogue's readers, and every change made
/// straight on the eval the page holds.
/// </summary>
/// <remarks>
/// Parameters down: the page hands the eval and the catalogue, and this changes the eval in place (a class the page
/// owns, ADR 0040); what a field is and how it becomes JSON is the plain classes' (ADR 0034).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0034-code-a-person-can-maintain.md">ADR 0034: code a person can maintain</seealso>
public partial class ComposeChecks
{
    /// <summary>The eval being composed.</summary>
    [Parameter, EditorRequired]
    public EvalComposition Eval { get; set; } = null!;

    /// <summary>The runner's checks and readers.</summary>
    [Parameter, EditorRequired]
    public CheckListReply Catalogue { get; set; } = null!;

    /// <summary>The type picked to add next.</summary>
    private string _adding = "";

    /// <summary>The check's catalogue entry, if the runner has its type.</summary>
    private CheckTypeRow? TypeOf(CheckEntry check) => Catalogue.Checks.FirstOrDefault(c => c.Type == check.Type);

    /// <summary>The check's fields, from its schema; none for a type the catalogue doesn't have.</summary>
    private IReadOnlyList<SchemaField> FieldsOf(CheckEntry check) =>
        TypeOf(check) is { } type ? SchemaFields.Of(type) : [];

    /// <summary>The check's kind, or a word that the catalogue doesn't know its type.</summary>
    private string KindOf(CheckEntry check) => TypeOf(check)?.Kind ?? "not in the catalogue";

    /// <summary>A field's label: its name, marked when the check needs it.</summary>
    private static string FieldLabel(SchemaField field) => field.Required ? $"{field.Name} *" : field.Name;

    /// <summary>What a field's input hints: its default, or for a list, commas.</summary>
    private static string Hint(SchemaField field) => field.Kind switch
    {
        FieldKind.List => "words, commas between",
        FieldKind.YesNo => field.Default ?? "true or false",
        _ => field.Default ?? "",
    };

    /// <summary>The value typed for a field so far.</summary>
    private static string ValueOf(CheckEntry check, SchemaField field) =>
        check.Values.GetValueOrDefault(field.Name, "");

    /// <summary>Keeps what was typed for a field on the eval.</summary>
    private static void Set(CheckEntry check, SchemaField field, ChangeEventArgs e) =>
        check.Values[field.Name] = e.Value?.ToString() ?? "";

    /// <summary>Adds a check of the picked type.</summary>
    private void Add()
    {
        Eval.Checks.Add(new CheckEntry { Type = _adding });
        _adding = "";
    }

    /// <summary>Removes a check.</summary>
    private void Remove(CheckEntry check) => Eval.Checks.Remove(check);
}
