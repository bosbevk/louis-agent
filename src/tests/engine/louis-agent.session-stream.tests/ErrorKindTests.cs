using System.Text.Json;
using LouisAgent.Choices;

namespace LouisAgent.SessionStream.Tests;

/// <summary>
/// A failed turn's error event says what kind of failure it was, in its <c>error.type</c> (FX26): the API writes it,
/// and a reader such as the eval runner reads it once as an enum. A kind this reader doesn't know reads as none.
/// </summary>
public class ErrorKindTests
{
    /// <summary>A failed turn's error event with this <c>error.type</c>, as the API streams it.</summary>
    private static ErrorReported Error(string type) => (ErrorReported)SessionEvent.From("error",
        JsonDocument.Parse($$$"""{"type":"error","error":{"type":"{{{type}}}","message":"it failed"}}""").RootElement
            .Clone());

    /// <summary>Each kind the API writes, by its wire name, reads as its member.</summary>
    [TestCase(StreamErrorKind.ReplayMiss)]
    [TestCase(StreamErrorKind.Api)]
    public void AnErrorsType_ReadsAsItsKind(StreamErrorKind kind) =>
        Assert.That(Error(kind.Wire()).Kind, Is.EqualTo(kind));

    /// <summary>A type this reader doesn't know, or an error event without one, reads as no kind at all.</summary>
    [Test]
    public void AnUnknownType_OrNone_ReadsAsNoKind()
    {
        var noType = (ErrorReported)SessionEvent.From("error",
            JsonDocument.Parse("""{"type":"error","error":{"message":"it failed"}}""").RootElement.Clone());

        Assert.That((Error("overloaded_error").Kind, noType.Kind),
            Is.EqualTo(((StreamErrorKind?)null, (StreamErrorKind?)null)));
    }
}
