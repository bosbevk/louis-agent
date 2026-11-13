using System.Net;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin;

/// <summary>
/// What every area's card on the admin's home does around its own read (F144): loads the admin key, reads, tells the
/// home when the key is refused, and reads again once a new key is saved. A card writes only what it reads.
/// </summary>
/// <remarks>
/// Template Method (Gang of Four): the steps are fixed here and a card fills in <see cref="ReadAsync"/>, so the
/// overview's, the packs' and the evals' cards can't each handle a refused key their own way. Without it each would
/// repeat the same try, catch and listener, and one would forget to read again after a key is saved.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/template-method">Template Method (Refactoring.Guru)</seealso>
public abstract class AdminCardBase : ComponentBase, IDisposable
{
    /// <summary>The API the card reads.</summary>
    [Inject]
    protected AgentApi Api { get; set; } = null!;

    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private AdminAccess Access { get; set; } = null!;

    /// <summary>The card couldn't read: no key, a wrong one, or no API.</summary>
    protected bool NotRead { get; private set; }

    /// <summary>The card's own read; it throws when the API refuses or can't be reached.</summary>
    protected abstract Task ReadAsync();

    /// <summary>Listens for a saved key, loads the key the first time, then reads.</summary>
    protected override async Task OnInitializedAsync()
    {
        Store.AdminKeySaved += OnKeySaved;
        if (!Store.Loaded) await Store.LoadAsync();
        await LoadAsync();
    }

    /// <summary>Stops listening, since the store outlives the card.</summary>
    public virtual void Dispose()
    {
        Store.AdminKeySaved -= OnKeySaved;
        GC.SuppressFinalize(this);
    }

    /// <summary>Reads; a refused key is told to the home, and any failure leaves the card quiet.</summary>
    private async Task LoadAsync()
    {
        NotRead = false;
        try
        {
            await ReadAsync();
        }
        catch (AgentApiException ex)
        {
            NotRead = true;
            if (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) Access.Refuse(ex.Message);
        }
        catch (HttpRequestException)
        {
            NotRead = true;
        }
    }

    /// <summary>A new key may open what the last one couldn't.</summary>
    private void OnKeySaved() => _ = InvokeAsync(async () =>
    {
        await LoadAsync();
        StateHasChanged();
    });
}
