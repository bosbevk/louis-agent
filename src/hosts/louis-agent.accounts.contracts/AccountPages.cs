namespace LouisAgent.Accounts.Contracts;

/// <summary>
/// The addresses of the framework's account pages, named once so the server can send a browser to them (the
/// confirmation link's landing) and the web draws them at the same place.
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
