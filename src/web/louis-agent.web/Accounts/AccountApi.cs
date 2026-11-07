using System.Net;
using System.Net.Http.Json;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Api.Contracts;

namespace LouisAgent.Web.Accounts;

/// <summary>
/// How the browser app talks to the <c>/account</c> routes, which the same API serves: register and confirm, sign in
/// and out with the cookie, forgot and reset, the profile, and the providers. Every call carries the header that
/// makes the cookie count (<see cref="ApiHeaders"/>); a refusal is an <see cref="AccountApiException"/> with what the
/// route said.
/// </summary>
/// <remarks>
/// Gateway (Fowler, PoEAA), as <c>AgentApi</c> is for the agent's routes: one class knows the addresses, the header,
/// the error shape and the wire's options, and every page goes through it. The bodies and replies are the contracts'
/// records (ADR 0045).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed class AccountApi(HttpClient http)
{
    /// <summary>Who is signed in, or null when nobody is (a 401) or the instance has no accounts (a 404).</summary>
    public async Task<AccountInfo?> MeAsync()
    {
        using var response = await SendAsync(HttpMethod.Get, "account/me", refuse: false);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound) return null;
        await ThrowIfRefusedAsync(response);
        return await response.Content.ReadFromJsonAsync<AccountInfo>(AccountJson.Web);
    }

    /// <summary>Whether this instance has accounts at all: the providers' list answers, or there's no such route.</summary>
    public async Task<bool> EnabledAsync()
    {
        using var response = await SendAsync(HttpMethod.Get, "account/providers", refuse: false);
        return response.IsSuccessStatusCode;
    }

    public Task RegisterAsync(RegisterBody body) => PostAsync("account/register", body);

    /// <summary>Signs in with the cookie; a refusal carries Identity's code (Failed, LockedOut, NotAllowed).</summary>
    public Task LoginAsync(LoginBody body) => PostAsync("account/login?useCookies=true", body);

    public Task LogoutAsync() => PostAsync("account/logout", null);

    public Task ResendConfirmationAsync(ResendConfirmationBody body) =>
        PostAsync("account/resendConfirmationEmail", body);

    /// <summary>Follows a confirmation link's parts: the API answers plain text, which nobody needs to read.</summary>
    public async Task ConfirmEmailAsync(string userId, string code, string? changedEmail)
    {
        string url = $"account/confirmEmail?userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(code)}";
        if (changedEmail is not null) url += $"&changedEmail={Uri.EscapeDataString(changedEmail)}";
        using var _ = await SendAsync(HttpMethod.Get, url);
    }

    public Task ForgotPasswordAsync(ForgotPasswordBody body) => PostAsync("account/forgotPassword", body);

    public Task ResetPasswordAsync(ResetPasswordBody body) => PostAsync("account/resetPassword", body);

    /// <summary>A new email (confirmed again) or a new password with the old one.</summary>
    public Task ChangeInfoAsync(InfoBody body) => PostAsync("account/manage/info", body);

    public async Task<AccountInfo> UpdateProfileAsync(ProfileBody body)
    {
        using var response = await SendAsync(HttpMethod.Put, "account/profile", body);
        return (await response.Content.ReadFromJsonAsync<AccountInfo>(AccountJson.Web))!;
    }

    public async Task DeleteAccountAsync()
    {
        using var _ = await SendAsync(HttpMethod.Delete, "account");
    }

    public async Task<ProvidersReply> ProvidersAsync()
    {
        using var response = await SendAsync(HttpMethod.Get, "account/providers");
        return (await response.Content.ReadFromJsonAsync<ProvidersReply>(AccountJson.Web))!;
    }

    private async Task PostAsync(string url, object? body)
    {
        using var _ = await SendAsync(HttpMethod.Post, url, body);
    }

    /// <summary>One call with the header, the body as JSON, and a refusal thrown with what the route said.</summary>
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null,
        bool refuse = true)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add(ApiHeaders.RequestedWith, ApiHeaders.RequestedWithWeb);
        if (body is not null) request.Content = JsonContent.Create(body, options: AccountJson.Web);
        var response = await http.SendAsync(request);
        if (refuse) await ThrowIfRefusedAsync(response);
        return response;
    }

    /// <summary>A status outside 2xx becomes the exception the pages read.</summary>
    private static async Task ThrowIfRefusedAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var problem = AccountProblem.Read(await response.Content.ReadAsStringAsync());
        response.Dispose();
        throw new AccountApiException(response.StatusCode, problem);
    }
}
