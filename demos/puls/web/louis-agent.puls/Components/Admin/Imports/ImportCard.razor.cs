using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Imports;
using LouisAgent.Puls.Admin.Imports;
using LouisAgent.Web.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace LouisAgent.Puls.Components.Admin.Imports;

/// <summary>
/// One pack's upload on the uploads page (F55): it reads the chosen files into its <see cref="ImportForm"/>, and posts
/// them to the upload's preview and save routes, showing the report each gives back.
/// </summary>
/// <remarks>
/// Presentation Model (Fowler): the rules of when a preview or a save may run live in <see cref="ImportForm"/>; this
/// component only reads files and calls the API.
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaDev/PresentationModel.html">Fowler: Presentation Model</seealso>
public partial class ImportCard
{
    /// <summary>The largest file the browser reads, the same cap the API keeps for a whole upload.</summary>
    private const long MaxBytes = 20 * 1024 * 1024;

    private bool _busy;
    private string? _error;

    [Inject] private AgentApi Api { get; set; } = null!;

    /// <summary>The upload this card shows, with its chosen files and last report.</summary>
    [Parameter, EditorRequired]
    public ImportForm Form { get; set; } = null!;

    /// <summary>Reads the file the person chose for a slot into the form.</summary>
    private async Task ChooseAsync(string slot, InputFileChangeEventArgs e)
    {
        _error = null;
        try
        {
            await using var stream = e.File.OpenReadStream(MaxBytes);
            using var bytes = new MemoryStream();
            await stream.CopyToAsync(bytes);
            Form.Choose(slot, e.File.Name, bytes.ToArray());
        }
        catch (IOException ex)
        {
            _error = ex.Message;
        }
    }

    /// <summary>Asks the API what the files hold and what saving them would change.</summary>
    private Task PreviewAsync() => PostAsync("preview");

    /// <summary>Keeps the previewed files.</summary>
    private Task SaveAsync() => PostAsync("save");

    /// <summary>Posts the files to one of the upload's routes and keeps the report; a refusal shows its reason.</summary>
    private async Task PostAsync(string route)
    {
        _busy = true;
        _error = null;
        try
        {
            Form.Report = await Api.PostAsync<ImportReportReply>(
                $"v1/admin/imports/{Uri.EscapeDataString(Form.Upload.Name)}/{route}", Form.Request(),
                AdminJson.SnakeCase);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            _error = ex.Message;
        }

        _busy = false;
    }
}
