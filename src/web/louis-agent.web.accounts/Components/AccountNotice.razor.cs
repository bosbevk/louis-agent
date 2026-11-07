using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Components;

/// <summary>A notice on an account page: a line, or a list of what didn't pass, in the card's own voice.</summary>
public partial class AccountNotice
{
    /// <summary>The line to say; none draws nothing.</summary>
    [Parameter]
    public string? Message { get; set; }

    /// <summary>The messages to list under the line, when a body didn't pass.</summary>
    [Parameter]
    public IReadOnlyList<string> Details { get; set; } = [];

    /// <summary>Whether it's bad news, drawn as such.</summary>
    [Parameter]
    public bool IsError { get; set; }
}
