using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Screens.Forms;

/// <summary>
/// One row as a form, drawn from what its pack described. The pack has the last word on what's saved, so a refusal
/// shows beside its field, and a field that can't change is never sent.
/// </summary>
/// <remarks>
/// Metadata-driven UI, as <see cref="Table.ScreenTable"/> is: the vocabulary is text, number, link and rows, and a new
/// kind enters only when a second screen needs it. It holds its own copy of the values while they're being typed, and
/// once saved the page reads the row again rather than trusting that copy (ADR 0042).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public partial class ScreenForm
{
    private Dictionary<string, string?> _values = [];
    private Dictionary<string, List<List<string>>> _rows = [];
    private ScreenRecordReply? _shown;
    private ScreenAnswerReply? _answer;
    private bool _saving;

    [Inject] private AgentApi Api { get; set; } = null!;

    /// <summary>The screen the row belongs to.</summary>
    [Parameter, EditorRequired] public string ScreenName { get; set; } = "";

    /// <summary>The row.</summary>
    [Parameter, EditorRequired] public string RowId { get; set; } = "";

    /// <summary>The row as its pack describes it.</summary>
    [Parameter, EditorRequired] public ScreenRecordReply Record { get; set; } = null!;

    /// <summary>It saved, so the page reads the row again.</summary>
    [Parameter] public EventCallback OnSaved { get; set; }

    /// <summary>Why a field was refused, if it was.</summary>
    private string? ErrorOf(ScreenFieldReply field) => _answer?.FieldErrors.GetValueOrDefault(field.Name);

    /// <summary>Takes a copy to type into, when a new description comes in rather than on every draw.</summary>
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_shown, Record))
            return;

        _shown = Record;
        _values = Record.Fields.Where(field => !IsRows(field)).ToDictionary(field => field.Name, field => field.Value);
        _rows = Record.Fields.Where(IsRows).ToDictionary(field => field.Name,
            field => field.Rows.Select(row => row.ToList()).ToList());
    }

    /// <summary>A field holding a list rather than one value.</summary>
    private static bool IsRows(ScreenFieldReply field) => ScreenFieldKinds.Of(field.Kind) is ScreenFieldKind.Rows;

    /// <summary>Sends what can change. A failure shows like any refusal, so the typing isn't lost.</summary>
    private async Task SaveAsync()
    {
        _saving = true;
        var editable = Record.Fields.Where(field => !field.ReadOnly).Select(field => field.Name).ToHashSet();
        var request = new ScreenSaveRequest(
            _values.Where(value => editable.Contains(value.Key)).ToDictionary(value => value.Key, value => value.Value),
            _rows.Where(rows => editable.Contains(rows.Key)).ToDictionary(rows => rows.Key,
                rows => (IReadOnlyList<IReadOnlyList<string>>)rows.Value));
        string address = $"v1/admin/screens/{Uri.EscapeDataString(ScreenName)}/rows/{Uri.EscapeDataString(RowId)}";
        try
        {
            _answer = await Api.PutAsync<ScreenAnswerReply>(address, request, AdminJson.SnakeCase);
        }
        catch (AgentApiException ex)
        {
            _answer = new ScreenAnswerReply(false, ex.Message, new Dictionary<string, string>());
        }
        catch (HttpRequestException ex)
        {
            _answer = new ScreenAnswerReply(false, ex.Message, new Dictionary<string, string>());
        }

        _saving = false;
        if (_answer.Done) await OnSaved.InvokeAsync();
    }
}
