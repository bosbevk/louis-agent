using System.Net.Http.Headers;
using System.Net.Http.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Leads.Messages;

namespace LouisAgent.Packs.Leads.Sending;

/// <summary>
/// Sends a lead through Resend's HTTP API (F112, Louis's pick C): one JSON post with the key as a bearer token, and
/// no package between us and the wire.
/// </summary>
/// <remarks>
/// The real Strategy behind <see cref="ILeadSender"/>. A refusal from the service becomes an
/// <see cref="HttpRequestException"/> with the status and the body, which the tool turns into text for the model.
/// </remarks>
public sealed class ResendLeadSender(HttpClient http, LeadsOptions options) : ILeadSender
{
    /// <summary>Where Resend takes a mail.</summary>
    public static readonly Uri Endpoint = new("https://api.resend.com/emails");

    /// <inheritdoc />
    public bool Mails => true;

    /// <inheritdoc />
    public async Task SendAsync(Lead lead, CancellationToken cancellationToken)
    {
        // The key goes in the header, never in the address or the body.
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(ResendMessage.Of(lead, options), options: ResendJson.Options)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        using var response = await http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode) return;

        // The service's own words come back with the status, so the reason reaches the model as text.
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(MessageText.Format(LeadsErrorMessage.ServiceSaidNo,
            new
            {
                Status = (int)response.StatusCode,
                Body = body
            }));
    }
}
