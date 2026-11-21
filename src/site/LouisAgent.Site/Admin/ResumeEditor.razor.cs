using System.Net;
using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.Basics;
using LouisAgent.Site.Resumes.Drafts;
using LouisAgent.Site.Resumes.Sources;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Admin;

/// <summary>
/// The editor for Louis's CV in the admin (F142): the CV's own page, with a small form beside it for what the page
/// doesn't draw. It opens on the copy the API holds, keeps every edit in a draft, and sends the draft when he saves.
/// </summary>
/// <remarks>
/// The page keeps the layout and the talk with the API. What is being edited lives in a <see cref="ResumeDraft"/>
/// (Presentation Model, Fowler), and each part hands back a new copy of itself, so the page changes the draft in one
/// place and the sheet can't show anything the draft doesn't hold.
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaDev/PresentationModel.html">Presentation Model (Fowler)</seealso>
public partial class ResumeEditor
{
    private ResumeDraft? _draft;
    private string? _error;
    private bool _needsKey, _saving, _saved;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;

    /// <summary>The admin's key first, for the save, then the CV.</summary>
    protected override async Task OnInitializedAsync()
    {
        // The frame has no chat that would have loaded the key already.
        if (!Store.Loaded) await Store.LoadAsync();
        await LoadAsync();
    }

    /// <summary>
    /// Starts a draft from the copy the API holds. It is read on the admin's own route, so without the admin's key
    /// there is no draft and nothing is drawn but the place to enter the key (FX153).
    /// </summary>
    private async Task LoadAsync()
    {
        _error = null;
        try
        {
            var held = await Api.GetAsync<Resume>(ResumeAddress.Admin, ResumeReader.Options);
            _draft = new ResumeDraft(held);
        }
        catch (AgentApiException ex)
        {
            _needsKey = ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            _error = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            _needsKey = false;
            _error = ex.Message;
        }
    }

    /// <summary>
    /// Sends the draft. What the API kept becomes the saved CV; a refusal is shown in the API's own words and the
    /// draft stays, to be fixed and sent again.
    /// </summary>
    private async Task SaveAsync()
    {
        if (_draft is not { } draft) return;
        _saving = true;
        _error = null;

        try
        {
            var kept = await Api.PutAsync<Resume>(ResumeAddress.Admin, draft.Current, ResumeReader.Options);
            draft.Kept(kept);
            _saved = true;
        }
        catch (AgentApiException ex)
        {
            _needsKey = ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            _error = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            _needsKey = false;
            _error = ex.Message;
        }

        _saving = false;
    }

    /// <summary>An edit goes into the draft, which is then no longer the saved CV.</summary>
    private void Edit(Resume edited)
    {
        _draft?.Edit(edited);
        _saved = false;
    }

    /// <summary>The top was edited.</summary>
    private void EditBasics(ResumeBasics basics) => Edit(_draft!.Current with { Basics = basics });

    /// <summary>Drops the edits, and any refusal that was about them.</summary>
    private void Discard()
    {
        _draft?.Discard();
        _error = null;
        _saved = false;
    }

    /// <summary>
    /// A key was saved. With no draft yet, it was the editor that didn't open, so it is opened now. With a draft, it
    /// was a save that was refused: the refusal goes, and Save can be pressed again.
    /// </summary>
    private Task KeySavedAsync()
    {
        _error = null;
        return _draft is null ? LoadAsync() : Task.CompletedTask;
    }
}
