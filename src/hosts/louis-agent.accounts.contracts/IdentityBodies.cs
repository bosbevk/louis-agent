namespace LouisAgent.Accounts.Contracts;

/// <summary>
/// What Identity's own routes under <c>/account</c> read, as records a browser app can send: the same property names
/// as Identity's request types, which live in the server's framework and can't be referenced from the browser. A
/// test pins each one to Identity's, so the two can't drift.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record RegisterBody(string Email, string Password);

/// <summary><c>POST /account/login?useCookies=true</c>; the two-factor fields only when the account has it on.</summary>
public sealed record LoginBody(
    string Email,
    string Password,
    string? TwoFactorCode = null,
    string? TwoFactorRecoveryCode = null);

/// <summary><c>POST /account/resendConfirmationEmail</c>.</summary>
public sealed record ResendConfirmationBody(string Email);

/// <summary><c>POST /account/forgotPassword</c>: the reset code goes to the mail seam.</summary>
public sealed record ForgotPasswordBody(string Email);

/// <summary><c>POST /account/resetPassword</c>, with the code from the mail.</summary>
public sealed record ResetPasswordBody(string Email, string ResetCode, string NewPassword);

/// <summary><c>POST /account/manage/info</c>: a new email (confirmed again) or a new password, with the old one.</summary>
public sealed record InfoBody(string? NewEmail = null, string? NewPassword = null, string? OldPassword = null);
