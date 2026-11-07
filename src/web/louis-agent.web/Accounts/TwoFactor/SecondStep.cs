using LouisAgent.Accounts.Contracts;

namespace LouisAgent.Web.Accounts.TwoFactor;

/// <summary>
/// The sign-in's second step (F61.5): one box takes either the app's six digits or a recovery code, and this puts
/// what was typed in the right field of the login body, so the form doesn't ask which kind it is.
/// </summary>
public static class SecondStep
{
    /// <summary>Digits only is the app's code; anything else (Identity's codes have a dash) is a recovery code.</summary>
    public static LoginBody Of(string email, string password, string code)
    {
        string trimmed = code.Trim();
        return trimmed.All(char.IsAsciiDigit)
            ? new LoginBody(email, password, TwoFactorCode: trimmed)
            : new LoginBody(email, password, TwoFactorRecoveryCode: trimmed);
    }
}
