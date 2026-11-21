using System.Net;
using System.Text.Json;
using LouisAgent.Packs.Leads;
using LouisAgent.Packs.Leads.Sending;
using LouisAgent.Packs.Leads.Tools;

namespace LouisAgent.Packs.Leads.Tests;

/// <summary>
/// The Resend sender (F112): one post to Resend's address with the key as a bearer token and the mail as snake_case
/// JSON; a refusal comes back as an exception with the status and the service's words, which the tool turns into text.
/// </summary>
public class ResendLeadSenderTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 8, 12, 40, 0, TimeSpan.Zero);
    private static readonly Lead Thandi = new("Thandi", "082 000 0000", "a bakery in Paarl", At);

    private static LeadsOptions Options() => new()
    {
        To = "louis@example.test",
        From = "guide@louis-agent.dev",
        ApiKey = "re_test_key"
    };

    private static (ResendLeadSender Sender, StubHandler Handler) Sender(HttpStatusCode status,
        string body = "{\"id\":\"1\"}")
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });
        return (new ResendLeadSender(new HttpClient(handler), Options()), handler);
    }

    /// <summary>The request is one post to Resend's address, with the key as a bearer token.</summary>
    [Test]
    public async Task ItPosts_ToResend_WithTheKey()
    {
        var (sender, handler) = Sender(HttpStatusCode.OK);
        string? authorization = null;
        var seeing = new StubHandler(request =>
        {
            authorization = request.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var seen = new ResendLeadSender(new HttpClient(seeing), Options());

        await seen.SendAsync(Thandi, CancellationToken.None);
        await sender.SendAsync(Thandi, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests, Has.Count.EqualTo(1));
            Assert.That(handler.Requests[0].Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Requests[0].Url, Is.EqualTo("https://api.resend.com/emails"));
            Assert.That(authorization, Is.EqualTo("Bearer re_test_key"));
        });
    }

    /// <summary>The body is the mail as snake_case JSON: from, to, subject and the three fields in the text.</summary>
    [Test]
    public async Task TheBody_IsTheMail_AsSnakeCaseJson()
    {
        var (sender, handler) = Sender(HttpStatusCode.OK);

        await sender.SendAsync(Thandi, CancellationToken.None);

        using var body = JsonDocument.Parse(handler.Requests[0].Body!);
        var root = body.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("from").GetString(), Is.EqualTo("guide@louis-agent.dev"));
            Assert.That(root.GetProperty("to")[0].GetString(), Is.EqualTo("louis@example.test"));
            Assert.That(root.GetProperty("subject").GetString(), Is.EqualTo("Lead from the site: Thandi"));
            Assert.That(root.GetProperty("text").GetString(),
                Does.Contain("Reach them: 082 000 0000").And.Contain("Wants: a bakery in Paarl").And
                    .Contain("2026-11-08 12:40:00Z"));
        });
    }

    /// <summary>
    /// A demonstration's lead (F127.2, Louis's pick "mails you, marked demo") is marked in its subject, with the
    /// business it was tried on, and in its body, so a try-out on the showroom never reads as a real customer's.
    /// </summary>
    [Test]
    public async Task ADemoLead_IsMarkedDemo_InItsSubjectAndBody()
    {
        var (sender, handler) = Sender(HttpStatusCode.OK);

        await sender.SendAsync(Thandi with
        {
            Identity = "seemeeu-guesthouse",
            Demonstration = true
        },
            CancellationToken.None);

        using var body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Multiple(() =>
        {
            Assert.That(body.RootElement.GetProperty("subject").GetString(),
                Is.EqualTo("[demo] seemeeu-guesthouse: Thandi"));
            Assert.That(body.RootElement.GetProperty("text").GetString(),
                Does.Contain("Wants: a bakery in Paarl").And.Contain("a showroom demo"));
        });
    }

    /// <summary>A refusal carries the status and the service's words, and the tool hands them to the model as text.</summary>
    [Test]
    public async Task ARefusal_BecomesTheToolsFailedText()
    {
        var (sender, _) = Sender(HttpStatusCode.UnprocessableEntity,
            "{\"message\":\"The from address is not verified\"}");
        var tools = new LeadTools(sender, TimeProvider.System);

        string answer = await tools.SendLead("Thandi", "082 000 0000", "a bakery in Paarl");

        Assert.That(answer,
            Does.StartWith("Not sent: the mail service answered 422 from the mail service: " +
                           "{\"message\":\"The from address is not verified\"}"));
    }
}
