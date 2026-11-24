using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.IntegrationTests.Llm.Documents;

/// <summary>
/// The proof a tool that reads a file stands on (F171.1 step 2): a Claude model, built as a host builds it, reads a
/// photo and a PDF sent as <see cref="DataContent" /> and answers as a typed record when asked with
/// <c>GetResponseAsync&lt;T&gt;</c>. The account it reads is made up and shows no real person or account. Two live
/// calls on a small model, each well under a US cent, and a third on a real bill kept outside the repository.
/// </summary>
/// <remarks>
/// There is no code behind it to see fail first: it proves the provider's adapter, not ours. If either reading fails,
/// the plan stops here.
/// </remarks>
[Category("Integration")]
public class DocumentAdapterProofTests
{
    /// <summary>The small model the proof runs on.</summary>
    private const string Model = "claude-haiku-5-5";

    /// <summary>The account number printed on the made-up account.</summary>
    private const string AccountNumber = "9876543210";

    /// <summary>The total due printed on it, in rand.</summary>
    private const decimal TotalDue = 3005.00m;

    /// <summary>What the proof asks the model to read off the account.</summary>
    private const string Ask =
        "Read this municipal account. Give its account number, digits only, and its total due in rand.";

    /// <summary>A small record of the proof's own: what a reading of the account gives back.</summary>
    private sealed record AccountReading(string AccountNumber, decimal TotalDue);

    /// <summary>A photo of the account is read into the record.</summary>
    [Test]
    public async Task APhotoOfTheAccount_IsReadIntoARecord() =>
        await AssertRead("sample-account.jpg", "image/jpeg");

    /// <summary>The account as a PDF is read into the record too.</summary>
    [Test]
    public async Task TheAccountAsAPdf_IsReadIntoARecord() =>
        await AssertRead("sample-account.pdf", "application/pdf");

    /// <summary>
    /// A real municipal bill reads too: busier than the made-up one, and a real layout. Its path is in
    /// <c>BILL_PDF</c>, a file on the machine that runs it, never in the repository, which is public; without it the
    /// test skips. Its numbers can't be written here, so it checks only that an account number and a total come back.
    /// </summary>
    [Test]
    public async Task ARealBill_IsReadIntoARecord()
    {
        string path = Outside.LocalFile("BILL_PDF", "real bill");

        var reading = await Read(path, "application/pdf");

        Assert.Multiple(() =>
        {
            Assert.That(reading.AccountNumber, Does.Match("^[0-9]+$"));
            Assert.That(reading.TotalDue, Is.Not.EqualTo(0m));
        });
    }

    /// <summary>Reads one of the made-up samples, and checks the numbers printed on it.</summary>
    private static async Task AssertRead(string file, string mediaType)
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Llm", "Documents", file);

        var reading = await Read(path, mediaType);

        Assert.Multiple(() =>
        {
            Assert.That(reading.AccountNumber, Is.EqualTo(AccountNumber));
            Assert.That(reading.TotalDue, Is.EqualTo(TotalDue));
        });
    }

    /// <summary>Sends the file with the ask in one user message, and gives back the record that comes back.</summary>
    private static async Task<AccountReading> Read(string path, string mediaType)
    {
        string key = Outside.Key("ANTHROPIC_API_KEY", "the documents' model");
        var options = new LlmOptions { Provider = LlmOptions.Anthropic, Model = Model, ApiKey = key };
        var model = new LlmClientFactory([AnthropicProvider.Instance]).Create(options);

        // The ask as text and the file as its bytes with their media type, as a tool will send them.
        var document = new DataContent(await File.ReadAllBytesAsync(path), mediaType);
        var message = new ChatMessage(ChatRole.User, [new TextContent(Ask), document]);
        var response = await model.GetResponseAsync<AccountReading>([message]);

        // Only the tokens are written out: a real bill's numbers stay off the test's output.
        var usage = response.Usage;
        TestContext.Out.WriteLine($"tokens in {usage?.InputTokenCount}, out {usage?.OutputTokenCount}");
        return response.Result;
    }
}
