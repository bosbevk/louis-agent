using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// <c>stream</c> on <c>POST /v1/sessions</c> names the format the session's events are written in (F18 step 1f; ADR
/// 0024: an enum, parsed once in the request). Nothing sent is Claude; a name that is none of the format's names is
/// refused (the API's 400), before any engine is built. Only the parse is tested here: the endpoint's 400 has no test,
/// since the API has no endpoint tests yet, and the first one should take that case.
/// </summary>
public class StreamFormatChoiceTests
{
    /// <summary>Asking for no format gets Claude's.</summary>
    [Test]
    public void NoFormatAskedIsClaude() =>
        Assert.That(new CreateSessionRequest(null, null, null).StreamFormatOrNull(), Is.EqualTo(StreamFormat.Claude));

    /// <summary>The name is read however it is written, spaces around it too.</summary>
    [Test]
    public void TheNameIsReadWithItsCaseIgnored() =>
        Assert.That(new CreateSessionRequest(null, null, null, Stream: " CLAUDE ").StreamFormatOrNull(),
            Is.EqualTo(StreamFormat.Claude));

    /// <summary>A name we don't know is refused.</summary>
    [Test]
    public void AFormatNoneOfWhoseNamesFitsIsRefused() =>
        Assert.That(new CreateSessionRequest(null, null, null, Stream: "nope").StreamFormatOrNull(), Is.Null);

    /// <summary>A number is no name, even one that would be the enum's value.</summary>
    [Test]
    public void ANumberNamesNoFormat() =>
        Assert.That(new CreateSessionRequest(null, null, null, Stream: "0").StreamFormatOrNull(), Is.Null);
}
