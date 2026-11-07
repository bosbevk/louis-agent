using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Account;

/// <summary>A password field with show and hide, bound two ways to the form that holds it.</summary>
public partial class PasswordField
{
    private bool _shown;

    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>The label above the field.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>The password as typed.</summary>
    [Parameter]
    public string Value { get; set; } = "";

    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>A line under the field, such as the rules a new password must meet.</summary>
    [Parameter]
    public string? Hint { get; set; }

    /// <summary>What the browser may fill in: <c>current-password</c> or <c>new-password</c>.</summary>
    [Parameter]
    public string Autocomplete { get; set; } = "current-password";

    private Task Changed(ChangeEventArgs e) => ValueChanged.InvokeAsync(e.Value?.ToString() ?? "");

    private void Toggle() => _shown = !_shown;
}
