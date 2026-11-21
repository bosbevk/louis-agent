using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Evals.Protocol.Events.Run;

/// <summary>A mode's repeat is done, with the mode's pass rates over the repeats so far.</summary>
public sealed record ModeDoneEvent(DateTimeOffset T, string Mode, int Repeat, RatesInfo? Rates) : EvalEvent(T);
