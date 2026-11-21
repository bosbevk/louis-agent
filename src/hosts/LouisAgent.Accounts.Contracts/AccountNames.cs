namespace LouisAgent.Accounts.Contracts;

/// <summary>
/// The claims the sign-in cookie carries beyond Identity's own (the user id is <c>ClaimTypes.NameIdentifier</c>),
/// named once here so the accounts library writes them and the API reads them by the same name.
/// </summary>
public static class AccountClaims
{
    /// <summary>The tenant the account is in (ADR 0088).</summary>
    public const string Tenant = "tenant";
}

/// <summary>
/// The addresses of the framework's account pages, named once so the server can send a browser to them (the
/// confirmation link's landing, a provider's return) and the web draws them at the same place.
/// </summary>
public static class AccountPages
{
    public const string Login = "login";
    public const string Register = "register";

    /// <summary>Where a browser lands after following the confirmation mail's link: <c>?done=true</c> or <c>?failed=true</c>.</summary>
    public const string ConfirmEmail = "confirm-email";

    public const string ForgotPassword = "forgot-password";
    public const string ResetPassword = "reset-password";
    public const string Profile = "profile";
}
