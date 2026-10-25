using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Logging.Content;
using LouisAgent.Core.Skills.Providers;
using Microsoft.Extensions.AI;

namespace LouisAgent.AcpServer.Tests.Turns;

// F18 step 4f.2: Rider's host logged the raw parameters of every prompt and its first 300 characters. What a developer types into an IDE
// chat is still personal data once the host runs for anyone else, so by default the lines say that a prompt arrived and how long it was,
// as a keyed hash and a length; LOG_CONTENT=full shows the text on the developer's own machine, as before. Played through the scripted
// console of AcpTranscriptTests; the protocol on stdout is unchanged (AcpTranscript.json).
[NonParallelizable]
public class PromptHandlerLogTests
{
    private const string Words = "Ek is in Malmesbury, wat kan ek kook?";

    private static async Task<string> StderrOfAPromptAsync(LogContentMode mode)
    {
        TextWriter original = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        string folder = Directory.CreateTempSubdirectory("acp-log-").FullName.Replace('\\', '/');
        try
        {
            AgentLogging.Configure(null, mode);
            var engine = new AgentEngine(new CompositeSkillProvider(), new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Melktert."))), TestPaths.Agent());
            var console = new ScriptedConsole();
            Task run = new AcpServer(engine, usageInChat: false, console.Input, console.Output).RunAsync();

            console.Send("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":1}}""");
            await console.LinesAsync(1);
            console.Send($$$"""{"jsonrpc":"2.0","id":2,"method":"session/new","params":{"cwd":"{{{folder}}}"}}""");
            var lines = await console.LinesAsync(2);
            string session = JsonDocument.Parse(lines[1]).RootElement.GetProperty("result").GetProperty("sessionId").GetString()!;
            console.Send($$$"""{"jsonrpc":"2.0","id":3,"method":"session/prompt","params":{"sessionId":"{{{session}}}","prompt":[{"type":"text","text":"{{{Words}}}"}]}}""");
            console.End();
            await run.WaitAsync(TimeSpan.FromSeconds(30));
            return stderr.ToString();
        }
        finally
        {
            Console.SetError(original);
            AgentLogging.Configure(null);
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public async Task ByDefault_ThePromptAndItsRawParameters_AreHashesAndLengths()
    {
        string log = await StderrOfAPromptAsync(LogContentMode.Hashed);

        Assert.That(log, Does.Not.Contain("Malmesbury").And.Not.Contain("kook"));
        Assert.That(log, Does.Match(@"Raw parameters:\s*#[0-9a-f]{12} \(\d+\)"));
        Assert.That(log, Does.Match(@"Prompt: \d+ chars: #[0-9a-f]{12} \(\d+\)"), "the count of characters stays; the text is a hash");
    }

    [Test]
    public async Task UnderFull_TheyAreAsBefore()
    {
        string log = await StderrOfAPromptAsync(LogContentMode.Full);

        Assert.That(log, Does.Contain("Raw parameters:").And.Contain(Words));
        Assert.That(log, Does.Contain($"chars: {Words}"));
    }
}
