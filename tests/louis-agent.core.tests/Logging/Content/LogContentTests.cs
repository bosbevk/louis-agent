using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Logging.Content;

namespace LouisAgent.Core.Tests.Logging.Content;

// F18 step 4f.1: what a log line shows of a value. Hashed by default: a keyed hash and the length, so equal values in one process
// still match but nobody can reverse a hash by hashing a list of towns (the key is random per process and never written down).
// The mode is process-wide, so these run one at a time and put it back to the default.
[NonParallelizable]
public class LogContentTests
{
    private static readonly Dictionary<string, object?> Args = new() { ["area"] = "Malmesbury", ["limit"] = 5, ["near"] = new { lat = -33.46, lon = 18.73 }, ["note"] = null };

    [TearDown]
    public void TearDown() => AgentLogging.Configure(null);

    [Test]
    public void OneValue_HashesTheSameTwiceInAProcess_AndAnotherKeyHashesItDifferently()
    {
        AgentLogging.Configure(null, LogContentMode.Hashed);

        Assert.That(LogContent.Of("Malmesbury"), Is.EqualTo(LogContent.Of("Malmesbury")));
        Assert.That(LogContent.Of("Malmesbury"), Is.Not.EqualTo(LogContent.Of("Paarl")));
        byte[] one = new byte[32], other = [.. Enumerable.Repeat((byte)7, 32)];
        Assert.That(LogContent.Hash("Malmesbury", one), Is.Not.EqualTo(LogContent.Hash("Malmesbury", other)), "a plain SHA-256 would be the same everywhere");
    }

    [Test]
    public void TheShape_IsHashTwelveHexDigitsAndTheLength_AndNullIsNull()
    {
        AgentLogging.Configure(null, LogContentMode.Hashed);

        Assert.That(LogContent.Of("Malmesbury"), Does.Match(@"^#[0-9a-f]{12} \(10\)$"));
        Assert.That(LogContent.Of(""), Does.Match(@"^#[0-9a-f]{12} \(0\)$"));
        Assert.That(LogContent.Of(null), Is.EqualTo("null"));
        Assert.That(LogContent.Of("Malmesbury", cut: 3), Does.Match(@"^#[0-9a-f]{12} \(10\)$"), "the cut is for the full text; the hash is of the whole value");
    }

    [Test]
    public void ThePlainSha256_IsNotWhatItWrites()
    {
        AgentLogging.Configure(null, LogContentMode.Hashed);
        string plain = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData("Malmesbury"u8))[..12];

        Assert.That(LogContent.Of("Malmesbury"), Does.Not.Contain(plain));
    }

    [Test]
    public void Full_ReturnsTheValue_CutWithThreeDotsWhereAsked()
    {
        AgentLogging.Configure(null, LogContentMode.Full);

        Assert.That(LogContent.Of("Malmesbury"), Is.EqualTo("Malmesbury"));
        Assert.That(LogContent.Of("Malmesbury", cut: 10), Is.EqualTo("Malmesbury"));
        Assert.That(LogContent.Of("Malmesbury", cut: 4), Is.EqualTo("Malm..."));
        Assert.That(LogContent.Of(null), Is.EqualTo("null"));
    }

    [Test]
    public void Full_Arguments_AreByteEqualToTheJsonOfToday()
    {
        AgentLogging.Configure(null, LogContentMode.Full);

        Assert.That(LogContent.OfArguments(Args), Is.EqualTo(JsonSerializer.Serialize(Args)));
        Assert.That(LogContent.OfArguments(new Dictionary<string, object?> { ["to"] = "Louis" }), Is.EqualTo("{\"to\":\"Louis\"}"));
        Assert.That(LogContent.OfArguments(null), Is.EqualTo("null"));
    }

    [Test]
    public void Hashed_Arguments_KeepTheNamesInOrder_AndShowNoValue()
    {
        AgentLogging.Configure(null, LogContentMode.Hashed);

        string text = LogContent.OfArguments(Args);

        Assert.That(text, Does.Match(@"^area=#[0-9a-f]{12} \(10\), limit=#[0-9a-f]{12} \(1\), near=#[0-9a-f]{12} \(\d+\), note=null$"));
        Assert.That(text, Does.Not.Contain("Malmesbury").And.Not.Contain("33.46").And.Not.Contain("18.73"));
        Assert.That(LogContent.OfArguments(new Dictionary<string, object?>()), Is.EqualTo("{}"));
        Assert.That(LogContent.OfArguments(null), Is.EqualTo("null"));
    }

    [Test]
    public void Hashed_Arguments_HashAStringAsItsText_AndAnythingElseAsItsJson()
    {
        AgentLogging.Configure(null, LogContentMode.Hashed);

        string text = LogContent.OfArguments(new Dictionary<string, object?> { ["area"] = "Malmesbury", ["limit"] = 5, ["near"] = new { lat = 1 } });

        Assert.That(text, Does.Contain($"area={LogContent.Of("Malmesbury")}").And.Contain($"limit={LogContent.Of("5")}").And.Contain($"near={LogContent.Of("{\"lat\":1}")}"));
    }

    [TestCase(null, LogContentMode.Hashed)]
    [TestCase("", LogContentMode.Hashed)]
    [TestCase("  ", LogContentMode.Hashed)]
    [TestCase("hashed", LogContentMode.Hashed)]
    [TestCase("FULL", LogContentMode.Full)]
    [TestCase(" full ", LogContentMode.Full)]
    public void ModeOf_ParsesTheSettingOnce_UnsetIsHashed(string? setting, LogContentMode expected) =>
        Assert.That(LogContent.ModeOf(setting), Is.EqualTo(expected));

    [TestCase("1")]
    [TestCase("secret")]
    public void ModeOf_RefusesAnythingElse_NamingTheSupportedValues(string setting)
    {
        var error = Assert.Throws<ArgumentException>(() => LogContent.ModeOf(setting))!;

        Assert.That(error.Message, Does.Contain($"'{setting}'").And.Contain("LOG_CONTENT").And.Contain("hashed or full"));
    }

    [Test]
    public void TheKey_SurvivesAnotherConfigure_SoLinesBeforeAndAfterHashAlike()
    {
        // AgentHost configures twice; a value logged between them must still match.
        AgentLogging.Configure(null, LogContentMode.Hashed);
        string before = LogContent.Of("Malmesbury");
        AgentLogging.Configure(null, LogContentMode.Hashed);

        Assert.That(LogContent.Of("Malmesbury"), Is.EqualTo(before));
    }
}
