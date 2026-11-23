using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Api.Contracts;
using LouisAgent.SessionStream;
using LouisAgent.SessionStream.Wire.Replies;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Evals.Sessions;

/// <summary>
/// Asks an agent over the sessions API, the way any app does: opens a session, sends a message and reads its stream
/// as it comes, and closes the session. When the API says no, you get a <see cref="SessionRefusedException"/> with
/// the API's own reason.
/// </summary>
/// <remarks>
/// One sealed class with no interface: a test hands it an <see cref="HttpClient"/> over a handler that answers, so
/// nothing needs a second implementation (ADR 0055). It writes and reads the shared wire records and reads the
/// stream with the shared reader, so nothing of the wire is typed twice (ADR 0045).
/// </remarks>
/// <param name="http">The client, with the API's address as its base and the instance's key on its headers.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed class SessionClient(HttpClient http)
{
    private const string Sessions = "v1/sessions";

    /// <summary>Opens a session as the request says, and hands back its id.</summary>
    public async Task<string> OpenAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(Sessions, request, JsonSerializerOptions.Web,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var opened =
            await response.Content.ReadFromJsonAsync<SessionCreatedReply>(SessionJson.SnakeCase, cancellationToken);
        return opened!.SessionId;
    }

    /// <summary>
    /// Sends one message, with the page it carries when one goes with it, and hands you its answer's events as they
    /// come.
    /// </summary>
    public async IAsyncEnumerable<SessionEvent> SendAsync(string session, string message, PageRequest? page = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Ask for the headers only, so the stream is read while the answer is still being written.
        var body = new SendMessageRequest(message, null, page);
        using var send = new HttpRequestMessage(HttpMethod.Post, $"{SessionAt(session)}/messages");
        send.Content = JsonContent.Create(body, options: JsonSerializerOptions.Web);
        using var response = await http.SendAsync(send, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, cancellationToken))
            yield return streamEvent;
    }

    /// <summary>Closes the session once its conversation is over.</summary>
    public async Task CloseAsync(string session, CancellationToken cancellationToken = default)
    {
        using var response = await http.DeleteAsync(SessionAt(session), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>The session's own address; its id is escaped, since it goes into the path.</summary>
    private static string SessionAt(string session) => $"{Sessions}/{Uri.EscapeDataString(session)}";

    /// <summary>Lets a good answer through, and turns any other into a refusal that says why.</summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        string text = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new SessionRefusedException((int)response.StatusCode, Reason(text, response.ReasonPhrase));
    }

    /// <summary>
    /// The best reason there is: the API's own message, else the body's text, else the status's reason phrase.
    /// </summary>
    /// <remarks>Guard clauses (Fowler): one source of the reason per line, the best first.</remarks>
    private static string Reason(string text, string? reasonPhrase)
    {
        if (ApiMessage(text) is { } message) return message;
        if (text.Trim().Length > 0) return text.Trim();
        return reasonPhrase ?? "";
    }

    /// <summary>The message in the API's error body, when the body is one.</summary>
    private static string? ApiMessage(string text)
    {
        try
        {
            return JsonSerializer.Deserialize<ApiError>(text, JsonSerializerOptions.Web)?.Error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
