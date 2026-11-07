using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Account.Profile;

/// <summary>The providers linked to the account, by name; F61.3 adds linking and unlinking.</summary>
public partial class LinkedLogins
{
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>The signed-in person.</summary>
    [Parameter, EditorRequired]
    public AccountInfo Me { get; set; } = null!;
}
