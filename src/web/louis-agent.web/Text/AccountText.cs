namespace LouisAgent.Web.Text;

/// <summary>
/// The words the account pages show (sign in, register, confirm, forgot, reset, the profile), so each site can show
/// them in its own language. English by default; a site registers its own copy, as it does for <see cref="ChatText"/>.
/// Templates use numbered holes for <see cref="string.Format(string, object?[])"/>.
/// </summary>
public sealed record AccountText
{
    // The pages' titles and the links between them.
    public string SignIn { get; init; } = "Sign in";
    public string SignInIntro { get; init; } = "Welcome back.";
    public string Register { get; init; } = "Create an account";
    public string RegisterIntro { get; init; } = "A few details and you're in.";
    public string NoAccountYet { get; init; } = "No account yet?";
    public string HaveAnAccount { get; init; } = "Already have an account?";
    public string ForgotPassword { get; init; } = "Forgot your password?";
    public string BackToSignIn { get; init; } = "Back to sign in";
    public string Or { get; init; } = "or";
    public string SignInWith { get; init; } = "Sign in with {0}";

    // The fields.
    public string Email { get; init; } = "Email";
    public string Password { get; init; } = "Password";
    public string ConfirmPassword { get; init; } = "Password again";
    public string NewPassword { get; init; } = "New password";
    public string CurrentPassword { get; init; } = "Current password";
    public string DisplayName { get; init; } = "Name";
    public string ShowPassword { get; init; } = "Show the password";
    public string HidePassword { get; init; } = "Hide the password";
    public string PasswordsDiffer { get; init; } = "The two passwords aren't the same.";

    public string PasswordRules { get; init; } =
        "At least 6 characters, with a capital, a small letter, a digit and a symbol.";

    // Registration and confirmation.
    public string Registered { get; init; } = "Nearly there: open the link we mailed to {0} to confirm it.";
    public string ResendConfirmation { get; init; } = "Send the link again";
    public string ConfirmationResent { get; init; } = "If that address has an account, a new link is on its way.";
    public string Confirming { get; init; } = "Confirming your email…";
    public string Confirmed { get; init; } = "Your email is confirmed. You can sign in now.";

    public string ConfirmFailed { get; init; } =
        "That link didn't work. It may have expired; ask for a new one from the sign-in page.";

    // Forgot and reset.
    public string ForgotIntro { get; init; } = "Tell us your email and we'll mail you a code to set a new password.";
    public string SendResetCode { get; init; } = "Send the code";
    public string ResetCodeSent { get; init; } = "If that address has an account, a code is on its way.";
    public string ResetPassword { get; init; } = "Set a new password";
    public string ResetCode { get; init; } = "The code from the mail";
    public string PasswordReset { get; init; } = "Your password is set. Sign in with it.";

    // Why a sign-in was refused, by Identity's codes.
    public string Failed { get; init; } = "That email and password don't match.";
    public string LockedOut { get; init; } = "Too many tries: this account is locked for a while.";
    public string NotAllowed { get; init; } = "Confirm your email first: open the link we mailed you.";

    public string RequiresTwoFactor { get; init; } =
        "Now the code from your authenticator app, or one of your recovery codes.";

    public string SomethingWentWrong { get; init; } = "Something went wrong. Try again in a moment.";

    // The profile.
    public string Profile { get; init; } = "Your profile";
    public string ProfileIntro { get; init; } = "What we call you, and how you sign in.";
    public string Save { get; init; } = "Save";
    public string Saved { get; init; } = "Saved.";
    public string EmailConfirmedYes { get; init; } = "confirmed";
    public string EmailConfirmedNo { get; init; } = "not confirmed yet";
    public string ChangeEmail { get; init; } = "Change your email";
    public string NewEmail { get; init; } = "New email";
    public string EmailChangeSent { get; init; } = "We mailed a link to {0}; open it and the change is made.";
    public string ChangePassword { get; init; } = "Change your password";
    public string SetPassword { get; init; } = "Set a password";
    public string PasswordChanged { get; init; } = "Your password is changed.";
    public string LinkedLogins { get; init; } = "Ways to sign in";
    public string NoLinkedLogins { get; init; } = "Only your password, so far.";
    public string LinkProvider { get; init; } = "Link {0}";
    public string Unlink { get; init; } = "Unlink";

    public string ExternalFailed { get; init; } =
        "Signing in through the provider didn't work. Try again, or use your password.";

    public string LinkFailed { get; init; } = "That provider couldn't be linked; it may already be on another account.";

    public string SetPasswordIntro { get; init; } =
        "You sign in through a provider. A password of your own is a second way in.";

    public string PasswordSet { get; init; } = "Your password is set.";

    // Passkeys (F61.5).
    public string Passkeys { get; init; } = "Passkeys";

    public string PasskeysIntro { get; init; } =
        "Sign in with your fingerprint, your face or a security key, no password to type.";

    public string NoPasskeys { get; init; } = "None yet.";
    public string PasskeyName { get; init; } = "A name for it (this laptop, my phone)";
    public string AddPasskey { get; init; } = "Add a passkey";
    public string PasskeyAdded { get; init; } = "The passkey is added.";
    public string RemovePasskey { get; init; } = "Remove";
    public string SignInWithPasskey { get; init; } = "Sign in with a passkey";
    public string PasskeysUnsupported { get; init; } = "This browser can't do passkeys.";
    public string PasskeyCancelled { get; init; } = "No passkey was given.";

    // Two-factor (F61.5).
    public string TwoFactor { get; init; } = "Two-factor sign-in";

    public string TwoFactorIntro { get; init; } =
        "A code from an authenticator app on your phone, asked for with your password.";

    public string TwoFactorSetup { get; init; } =
        "Add this key to your authenticator app (Microsoft Authenticator, Google Authenticator, Aegis), then type the code it shows.";

    public string OpenInApp { get; init; } = "Open in your authenticator app";
    public string AuthenticatorCode { get; init; } = "The code from the app";
    public string TurnOn { get; init; } = "Turn on";
    public string TurnOff { get; init; } = "Turn off";
    public string TwoFactorOn { get; init; } = "Two-factor sign-in is on.";
    public string TwoFactorOff { get; init; } = "Two-factor sign-in is off.";
    public string RecoveryCodesLeft { get; init; } = "{0} recovery codes left.";
    public string RecoveryCodes { get; init; } = "Your recovery codes";

    public string RecoveryCodesIntro { get; init; } =
        "Keep these somewhere safe: each signs you in once when the app isn't at hand. They're shown this once.";

    public string NewRecoveryCodes { get; init; } = "New recovery codes";

    public string DeleteAccount { get; init; } = "Delete your account";

    public string DeleteIntro { get; init; } =
        "Everything about you goes: your account, your name, your sign-ins. Type your email to confirm.";

    public string DeleteConfirmField { get; init; } = "Your email, to confirm";
    public string Delete { get; init; } = "Delete it";
    public string SignOut { get; init; } = "Sign out";
    public string Guest { get; init; } = "Guest";
    public string NeedsSignIn { get; init; } = "Sign in to chat.";
}
