using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts;
using LouisAgent.Web.Accounts.Errors;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Account.Profile;

/// <summary>The facts about the account, and the name the person goes by; saving the name updates the state.</summary>
public partial class ProfileDetails
{
    private string _name = "";
    private AccountErrorView? _notice;
    private bool _busy, _isError;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountState State { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>The signed-in person.</summary>
    [Parameter, EditorRequired]
    public AccountInfo Me { get; set; } = null!;

    protected override void OnParametersSet() => _name = Me.DisplayName ?? "";

    /// <summary>Sends the name; the API answers the whole account, which the state takes.</summary>
    private async Task SaveAsync()
    {
        _busy = true;
        _notice = null;
        try
        {
            State.Set(await Api.UpdateProfileAsync(new ProfileBody(_name)));
            _notice = new AccountErrorView(Text.Saved, []);
            _isError = false;
        }
        catch (Exception ex) when (ex is AccountApiException or HttpRequestException)
        {
            _notice = AccountErrors.Of(ex, Text);
            _isError = true;
        }
        finally
        {
            _busy = false;
        }
    }
}
