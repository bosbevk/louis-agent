using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Pages.Account;

/// <summary>The confirmation's landing: a yes from the query, or a no for anything else, a bad link included.</summary>
public partial class ConfirmEmailPage
{
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>The API sent the browser here with <c>?done=true</c>; anything else is a link that didn't work.</summary>
    [Parameter, SupplyParameterFromQuery]
    public bool Done { get; set; }
}
