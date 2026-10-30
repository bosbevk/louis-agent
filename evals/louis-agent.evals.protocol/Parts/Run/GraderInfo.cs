using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Protocol.Parts.Run;

/// <summary>
/// One of the eval's graders, as the run's first line names it. A human review also carries the question a person is
/// asked, so the portal shows what that run asked.
/// </summary>
public sealed record GraderInfo(string Name, int Version, CheckKind Kind, string? Question);
