using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Api.Sessions.Streaming;

namespace LouisAgent.Api.Tests.Sessions;

// F18 step 1f: "stream" on POST /sessions names the format the session's events are written in (ADR 0024: an enum, parsed
// once in the request). Nothing sent is Claude; a name that is none of the format's names is refused (the API's 400),
// before any engine is built.
public class StreamFormatChoiceTests
{
    [Test]
    public void NoFormatAskedIsClaude() =>
        Assert.That(new CreateSessionRequest(null, null, null).StreamFormatOrNull(), Is.EqualTo(StreamFormat.Claude));

    [Test]
    public void TheNameIsReadWithItsCaseIgnored() =>
        Assert.That(new CreateSessionRequest(null, null, null, Stream: " CLAUDE ").StreamFormatOrNull(), Is.EqualTo(StreamFormat.Claude));

    [Test]
    public void AFormatNoneOfWhoseNamesFitsIsRefused() =>
        Assert.That(new CreateSessionRequest(null, null, null, Stream: "nope").StreamFormatOrNull(), Is.Null);

    [Test]
    public void ANumberNamesNoFormat() =>
        Assert.That(new CreateSessionRequest(null, null, null, Stream: "0").StreamFormatOrNull(), Is.Null);
}
