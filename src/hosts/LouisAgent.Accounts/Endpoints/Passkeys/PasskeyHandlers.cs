using System.Buffers.Text;
using System.Security.Claims;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Accounts.Messages;
using LouisAgent.Accounts.Users;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace LouisAgent.Accounts.Endpoints.Passkeys;

/// <summary>
/// Passkeys (F61.5), on Identity's own support: the options the browser needs to make one, the new passkey itself,
/// the list and the removal for the signed-in person; and, for anyone, the options to sign in with one and the
/// sign-in. The state between an options call and the credential that answers it is Identity's to keep.
/// </summary>
/// <remarks>
/// Named static methods returning <c>TypedResults</c> (ADR 0051), as the other account routes; the options are
/// Identity's JSON passed through, since the browser hands them to <c>PublicKeyCredential.parseCreationOptionsFromJSON</c>
/// as they are.
/// </remarks>
public static class PasskeyHandlers
{
    /// <summary>The options for <c>navigator.credentials.create()</c>, for the signed-in person.</summary>
    public static async Task<Results<ContentHttpResult, UnauthorizedHttpResult>> CreationOptions(
        ClaimsPrincipal principal, UserManager<AccountUser> users, SignInManager<AccountUser> signIn)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return TypedResults.Unauthorized();

        string name = user.Email ?? user.UserName ?? user.Id;
        string options = await signIn.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = user.Id,
            Name = name,
            DisplayName = user.DisplayName ?? name
        });
        return TypedResults.Content(options, "application/json");
    }

    /// <summary>The credential the browser made, checked and kept under the name the person gave it.</summary>
    public static async Task<Results<NoContent, UnauthorizedHttpResult, ValidationProblem>> Add(
        ClaimsPrincipal principal, PasskeyBody body, UserManager<AccountUser> users, SignInManager<AccountUser> signIn)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return TypedResults.Unauthorized();

        // A credential that answers no options of ours (none were asked for, or the state is gone) is Identity's
        // exception, not a failed result; it's the same refusal to the person.
        PasskeyAttestationResult attestation;
        try
        {
            attestation = await signIn.PerformPasskeyAttestationAsync(body.CredentialJson);
        }
        catch (InvalidOperationException ex)
        {
            return AccountProblems.Of(nameof(body.CredentialJson), AccountUserText.PasskeyRefused,
                new { Reason = ex.Message });
        }

        if (!attestation.Succeeded)
            return AccountProblems.Of(nameof(body.CredentialJson), AccountUserText.PasskeyRefused,
                new { Reason = attestation.Failure.Message });

        attestation.Passkey.Name = string.IsNullOrWhiteSpace(body.Name) ? null : body.Name.Trim();
        var added = await users.AddOrUpdatePasskeyAsync(user, attestation.Passkey);
        if (!added.Succeeded) return AccountProblems.Of(added);
        return TypedResults.NoContent();
    }

    /// <summary>The person's passkeys, newest last.</summary>
    public static async Task<Results<JsonHttpResult<PasskeysReply>, UnauthorizedHttpResult>> List(
        ClaimsPrincipal principal, UserManager<AccountUser> users)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return TypedResults.Unauthorized();

        var passkeys = await users.GetPasskeysAsync(user);
        return TypedResults.Json(new PasskeysReply(passkeys
            .Select(p => new PasskeyInfo(Base64Url.EncodeToString(p.CredentialId), p.Name, p.CreatedAt))
            .ToList()), AccountJson.Web);
    }

    /// <summary>Takes a passkey off the account; one that isn't there is a 404.</summary>
    public static async Task<Results<NoContent, UnauthorizedHttpResult, NotFound>> Remove(string id,
        ClaimsPrincipal principal, UserManager<AccountUser> users)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return TypedResults.Unauthorized();
        if (!Base64Url.IsValid(id)) return TypedResults.NotFound();

        byte[] credentialId = Base64Url.DecodeFromChars(id);
        if (await users.GetPasskeyAsync(user, credentialId) is null) return TypedResults.NotFound();
        await users.RemovePasskeyAsync(user, credentialId);
        return TypedResults.NoContent();
    }

    /// <summary>The options for <c>navigator.credentials.get()</c>: for one person's passkeys when the email is given, else any.</summary>
    public static async Task<ContentHttpResult> RequestOptions(string? email, UserManager<AccountUser> users,
        SignInManager<AccountUser> signIn)
    {
        var user = string.IsNullOrWhiteSpace(email) ? null : await users.FindByEmailAsync(email.Trim());
        string options = await signIn.MakePasskeyRequestOptionsAsync(user);
        return TypedResults.Content(options, "application/json");
    }

    /// <summary>Signs in with the credential the browser got; a refusal says so in Identity's shape.</summary>
    public static async Task<Results<Ok, ProblemHttpResult>> Login(PasskeyLoginBody body,
        SignInManager<AccountUser> signIn)
    {
        var result = await signIn.PasskeySignInAsync(body.CredentialJson);
        if (result.Succeeded) return TypedResults.Ok();
        return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized,
            detail: result.IsLockedOut ? "LockedOut" : result.IsNotAllowed ? "NotAllowed" : "Failed");
    }
}
