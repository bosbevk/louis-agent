namespace LouisAgent.Core.Sessions.Replay.Store;

/// <summary>
/// What a recording holds for an ask: the value the seam answered (<see cref="Answered"/>) or the failure's text it showed the agent
/// (<see cref="Failed"/>), never both and never neither. <see cref="OrThrow"/> is the replay's answer: the value, or the same kind of
/// exception the agent saw, built by the seam from the recorded text.
/// </summary>
public abstract record ReplayAnswer<T>
{
    private ReplayAnswer() { }

    /// <summary>The recorded value; throws <paramref name="failure"/>(the recorded text) when the recording is a failure.</summary>
    public abstract T OrThrow(Func<string, Exception> failure);

    public sealed record Answered(T Value) : ReplayAnswer<T>
    {
        public override T OrThrow(Func<string, Exception> failure) => Value;
    }

    public sealed record Failed(string Error) : ReplayAnswer<T>
    {
        public override T OrThrow(Func<string, Exception> failure) => throw failure(Error);
    }
}
