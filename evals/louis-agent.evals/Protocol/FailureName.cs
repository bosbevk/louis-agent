using System.Text.Json;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Protocol;

/// <summary>A <see cref="TrialFailure"/> as the protocol's <c>turn</c> line names it: the enum's name in snake case (<c>api_error</c>).</summary>
public static class FailureName
{
    public static string Of(TrialFailure failure) => JsonNamingPolicy.SnakeCaseLower.ConvertName(failure.ToString());

    /// <summary>The failure a line names, or null when it names none this build knows.</summary>
    public static TrialFailure? Parse(string? name) =>
        Enum.GetValues<TrialFailure>().Cast<TrialFailure?>().FirstOrDefault(f => Of(f!.Value) == name);
}
