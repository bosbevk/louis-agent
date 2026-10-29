using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Logging.Content;

namespace LouisAgent.Core.Tests.Logging.Content;

/// <summary>
/// What a log line shows of a value. By default it's a keyed hash and the length, so the same value matches itself in
/// one process, but nobody can undo a hash by hashing a list of towns (the key is random per process and never written
/// down). The mode is process-wide, so these run one at a time and put it back to the default.
/// </summary>
[NonParallelizable]
public class LogContentTests
{
    private static readonly Dictionary<string, object?> Args = new()
    {
        ["area"] = "Malmesbury",
        ["limit"] = 5,
        ["near"] = new
        {
            lat = -33.46,
            lon = 18.73
        },
        ["note"] = null
    };

    /// <summary>
    /// Puts the logging back to its default after each test, so the next one starts clean.
    /// </summary>
    [TearDown]
    public void TearDown() => AgentLogging.Configure(null);

    /// <summary>
    /// The same value hashes the same twice in one process, and a different key gives a different hash.
    /// </summary>
    [Test]
    public void OneValue_HashesTheSameTwiceInAProcess_AndAnotherKeyHashesItDifferently()
    {
        // F18 step 4f.1.
        AgentLogging.Configure(null, LogContentMode.Hashed);

        string first = LogContent.Of("Malmesbury"), second = LogContent.Of("Malmesbury");
        Assert.That(second, Is.EqualTo(first));
        Assert.That(LogContent.Of("Malmesbury"), Is.Not.EqualTo(LogContent.Of("Paarl")));
        byte[] one = new byte[32], other = [.. Enumerable.Repeat((byte)7, 32)];
        Assert.That(LogContent.Hash("Malmesbury", one), Is.Not.EqualTo(LogContent.Hash("Malmesbury", other)),
            "a plain SHA-256 would be the same everywhere");
    }

    /// <summary>
    /// A hashed value reads as "#" with twelve hex digits and its length; a missing one just says null.
    /// </summary>
    [Test]
    public void TheShape_IsHashTwelveHexDigitsAndTheLength_AndNullIsNull()
    {
        AgentLogging.Configure(null, LogContentMode.Hashed);

        Assert.That(LogContent.Of("Malmesbury"), Does.Match(@"^#[0-9a-f]{12} \(10\)$"));
        Assert.That(LogContent.Of(""), Does.Match(@"^#[0-9a-f]{12} \(0\)$"));
        Assert.That(LogContent.Of(null), Is.EqualTo("null"));
        Assert.That(LogContent.Of("Malmesbury", cut: 3), Does.Match(@"^#[0-9a-f]{12} \(10\)$"),
            "the cut is for the full text; the hash is of the whole value");
    }

    /// <summary>
    /// The log never holds a plain SHA-256, which anyone could look up.
    /// </summary>
    [Test]
    public void ThePlainSha256_IsNotWhatItWrites()
    {
        AgentLogging.Configure(null, LogContentMode.Hashed);
        string plain = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData("Malmesbury"u8))[..12];

        Assert.That(LogContent.Of("Malmesbury"), Does.Not.Contain(plain));
    }

    /// <summary>
    /// In full mode you see the value itself, cut short with three dots where the caller asks.
    /// </summary>
    [Test]
    public void Full_ReturnsTheValue_CutWithThreeDotsWhereAsked()
    {
        AgentLogging.Configure(null, LogContentMode.Full);

        Assert.That(LogContent.Of("Malmesbury"), Is.EqualTo("Malmesbury"));
        Assert.That(LogContent.Of("Malmesbury", cut: 10), Is.EqualTo("Malmesbury"));
        Assert.That(LogContent.Of("Malmesbury", cut: 4), Is.EqualTo("Malm..."));
        Assert.That(LogContent.Of(null), Is.EqualTo("null"));
    }

    /// <summary>
    /// In full mode a tool's arguments log exactly as the JSON they always did.
    /// </summary>
    [Test]
    public void Full_Arguments_AreByteEqualToTheJsonOfToday()
    {
        AgentLogging.Configure(null, LogContentMode.Full);

        Assert.That(LogContent.OfArguments(Args), Is.EqualTo(JsonSerializer.Serialize(Args)));
        Assert.That(LogContent.OfArguments(new Dictionary<string, object?> { ["to"] = "Louis" }),
            Is.EqualTo("{\"to\":\"Louis\"}"));
        Assert.That(LogContent.OfArguments(null), Is.EqualTo("null"));
    }

    /// <summary>
    /// Hashed arguments keep their names in order, so you can see what was passed, but never a value.
    /// </summary>
    [Test]
    public void Hashed_Arguments_KeepTheNamesInOrder_AndShowNoValue()
    {
        AgentLogging.Configure(null, LogContentMode.Hashed);

        string text = LogContent.OfArguments(Args);

        Assert.That(text,
            Does.Match(
                @"^area=#[0-9a-f]{12} \(10\), limit=#[0-9a-f]{12} \(1\), near=#[0-9a-f]{12} \(\d+\), note=null$"));
        Assert.That(text, Does.Not.Contain("Malmesbury").And.Not.Contain("33.46").And.Not.Contain("18.73"));
        Assert.That(LogContent.OfArguments(new Dictionary<string, object?>()), Is.EqualTo("{}"));
        Assert.That(LogContent.OfArguments(null), Is.EqualTo("null"));
    }

    /// <summary>
    /// A text argument hashes as the text itself; a number or an object hashes as its JSON.
    /// </summary>
    [Test]
    public void Hashed_Arguments_HashAStringAsItsText_AndAnythingElseAsItsJson()
    {
        AgentLogging.Configure(null, LogContentMode.Hashed);

        string text = LogContent.OfArguments(new Dictionary<string, object?>
        {
            ["area"] = "Malmesbury",
            ["limit"] = 5,
            ["near"] = new { lat = 1 }
        });

        Assert.That(text,
            Does.Contain($"area={LogContent.Of("Malmesbury")}").And.Contain($"limit={LogContent.Of("5")}").And
                .Contain($"near={LogContent.Of("{\"lat\":1}")}"));
    }

    /// <summary>
    /// The model's arguments arrive as JSON, and a town in them hashes the same as that town logged by the shops, so
    /// you can follow one through the log.
    /// </summary>
    [Test]
    public void Hashed_Arguments_FromTheModel_AreJsonElements_AndAStringHashesAsItsText()
    {
        // The model's arguments arrive as JsonElement, not string. Hashed as JSON, "Malmesbury" kept its quotes
        // (length 12) and hashed differently from the same town logged by the shops as a plain string, so one town
        // had two hashes in one log. The live check of 4f found it (#138099225789 (12)): a string element hashes as
        // its text, so the tool call and the shop's lines can be followed.
        AgentLogging.Configure(null, LogContentMode.Hashed);
        using var document =
            JsonDocument.Parse("""{"area":"Malmesbury","q":"café","limit":5,"near":{"lat":1},"on":true,"none":null}""");
        var arguments = document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value.Clone());

        string text = LogContent.OfArguments(arguments);

        Assert.That(text, Does.Contain($"area={LogContent.Of("Malmesbury")}").And.Contain("(10)"));
        Assert.That(text, Does.Contain($"q={LogContent.Of("café")}"),
            "the text, unescaped, not the serializer's \\u00e9");
        Assert.That(text,
            Does.Contain($"limit={LogContent.Of("5")}").And.Contain($"near={LogContent.Of("{\"lat\":1}")}").And
                .Contain($"on={LogContent.Of("true")}"),
            "anything else hashes as its JSON, as before");
        Assert.That(text, Does.Contain("none=null"), "a JSON null logs as null, as a missing argument does");
    }

    /// <summary>
    /// "Malmesbury" as JSON and "Malmesbury" as plain text log as the same hash.
    /// </summary>
    [Test]
    public void Hashed_Arguments_AJsonStringAndAPlainStringOfTheSameText_HashAlike()
    {
        AgentLogging.Configure(null, LogContentMode.Hashed);
        using var document = JsonDocument.Parse("\"Malmesbury\"");

        Assert.That(LogContent.OfArguments(new Dictionary<string, object?> { ["area"] = document.RootElement.Clone() }),
            Is.EqualTo(LogContent.OfArguments(new Dictionary<string, object?> { ["area"] = "Malmesbury" })));
    }

    /// <summary>
    /// LOG_CONTENT is read once, case and spaces ignored; left empty, it hashes.
    /// </summary>
    [TestCase(null, LogContentMode.Hashed)]
    [TestCase("", LogContentMode.Hashed)]
    [TestCase("  ", LogContentMode.Hashed)]
    [TestCase("hashed", LogContentMode.Hashed)]
    [TestCase("FULL", LogContentMode.Full)]
    [TestCase(" full ", LogContentMode.Full)]
    public void ModeOf_ParsesTheSettingOnce_UnsetIsHashed(string? setting, LogContentMode expected) =>
        Assert.That(LogContent.ModeOf(setting), Is.EqualTo(expected));

    /// <summary>
    /// Any other LOG_CONTENT is refused, with a message that names the two values it takes.
    /// </summary>
    [TestCase("1")]
    [TestCase("secret")]
    public void ModeOf_RefusesAnythingElse_NamingTheSupportedValues(string setting)
    {
        var error = Assert.Throws<ArgumentException>(() => LogContent.ModeOf(setting))!;

        Assert.That(error.Message,
            Does.Contain($"'{setting}'").And.Contain("LOG_CONTENT").And.Contain("hashed or full"));
    }

    /// <summary>
    /// Setting the logging up a second time keeps the key, so a value hashes the same before and after.
    /// </summary>
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
