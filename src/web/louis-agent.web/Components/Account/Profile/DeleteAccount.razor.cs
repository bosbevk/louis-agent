using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Account.Profile;

/// <summary>
/// Deletes the account once the person has typed their own email: the button stays off until it matches, so a slip
/// can't do it. Afterwards the state is cleared and the page that holds this hears it's done.
/// </summary>
public partial class DeleteAccount
{
    private string _typed = "";
    private string? _error;
    private bool _busy;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountState State { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>The signed-in person.</summary>
    [Parameter, EditorRequired]
    public AccountInfo Me { get; set; } = null!;

    /// <summary>Fires once the account is gone.</summary>
    [Parameter]
    public EventCallback OnDeleted { get; set; }

    /// <summary>Whether what was typed is the account's email, case aside.</summary>
    private bool Matches => string.Equals(_typed.Trim(), Me.Email, StringComparison.OrdinalIgnoreCase);

    private async Task DeleteAsync()
    {
        if (!Matches) return;
        _busy = true;
        _error = null;
        try
        {
            await Api.DeleteAccountAsync();
            State.Clear();
            await OnDeleted.InvokeAsync();
        }
        catch (Exception ex) when (ex is AccountApiException or HttpRequestException)
        {
            _error = Text.SomethingWentWrong;
        }
        finally
        {
            _busy = false;
        }
    }
}
