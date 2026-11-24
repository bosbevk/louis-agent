using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Llm.Replay;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Tests.Hosting.Catalogue;
using LouisAgent.Testing.Replay;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Llm.Replay;

/// <summary>
/// F171.1 step 9: an eval replays a recorded run without paying for it, so a tool's reading of a file is found again
/// by what it asked: the same instructions, file and record come back from the recording, and a file one byte
/// different, or another record asked for, is a miss, never a call to the model.
/// </summary>
public class ToolModelReplayTests
{
    /// <summary>The trial the tests record into and replay from.</summary>
    private const string Trial = "r1:a:1/t1";

    /// <summary>The account as the provider answers it.</summary>
    private const string Answer = """{"accountNumber":"9876543210","totalDue":3005.00}""";

    /// <summary>A fresh replay folder for each test.</summary>
    private ReplayFolder _folder = null!;

    /// <summary>Makes the test's replay folder.</summary>
    [SetUp]
    public void SetUp() => _folder = new ReplayFolder();

    /// <summary>Deletes the test's replay folder.</summary>
    [TearDown]
    public void TearDown() => _folder.Dispose();

    /// <summary>
    /// Another record a tool might ask for, so its schema differs from <see cref="SampleAccount" />'s.
    /// </summary>
    private sealed record OtherReading(string Holder);

    /// <summary>A trial, recording or replaying, for the length of a block.</summary>
    private static IDisposable Flow(WebReplayMode web, bool model) =>
        SessionReplay.Use(new SessionReplay(web, Trial, model));

    /// <summary>The instance's model as a tool is given it, over this provider and the test's recordings.</summary>
    private IChatClient Model(IChatClient provider) =>
        ToolModels.Create(provider, null, [], null, _folder.Store).Instance;

    /// <summary>A photo of the account, with its last byte as given.</summary>
    private static ChatMessage Reading(byte last) =>
        new(ChatRole.User, [new TextContent(ReadSampleTools.Instructions), new DataContent(
            new byte[] { 0xFF, 0xD8, 0x01, last }, "image/jpeg")]);

    /// <summary>A provider that fails the test if it is ever called.</summary>
    private static FakeChatClient Throwing() => new(_ => throw new InvalidOperationException("the model was called"));

    /// <summary>Records one reading of the photo whose last byte is 0x02.</summary>
    private async Task RecordAsync()
    {
        using (Flow(WebReplayMode.Record, model: false))
            await Model(new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, Answer))))
                .GetResponseAsync<SampleAccount>([Reading(0x02)]);
    }

    /// <summary>The same reading again is answered from the recording, and the provider is never called.</summary>
    [Test]
    public async Task TheSameReading_IsAnsweredFromTheRecording()
    {
        await RecordAsync();
        var throwing = Throwing();

        using (Flow(WebReplayMode.Replay, model: true))
        {
            var replayed = await Model(throwing).GetResponseAsync<SampleAccount>([Reading(0x02)]);

            Assert.That(replayed.Result, Is.EqualTo(new SampleAccount("9876543210", 3005.00m)));
        }

        Assert.That(throwing.Requests, Is.Empty);
    }

    /// <summary>The same instructions with one byte of the file changed is a miss.</summary>
    [Test]
    public async Task OneByteOfTheFileChanged_IsAMiss()
    {
        await RecordAsync();

        using (Flow(WebReplayMode.Replay, model: true))
            Assert.That(async () => await Model(Throwing()).GetResponseAsync<SampleAccount>([Reading(0x03)]),
                Throws.InstanceOf<ModelReplayMissException>());
    }

    /// <summary>The same file asked for another record, so another schema, is a miss.</summary>
    [Test]
    public async Task AnotherRecordAskedFor_IsAMiss()
    {
        await RecordAsync();

        using (Flow(WebReplayMode.Replay, model: true))
            Assert.That(async () => await Model(Throwing()).GetResponseAsync<OtherReading>([Reading(0x02)]),
                Throws.InstanceOf<ModelReplayMissException>());
    }
}
