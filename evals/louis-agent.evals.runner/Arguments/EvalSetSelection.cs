using LouisAgent.Core.Messages;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.Choices;

namespace LouisAgent.Evals.Runner.Arguments;

/// <summary>
/// <c>EVALS=puls</c>, comma separated, case ignored, each name once: the sets to load, as the composition's IDENTITIES picks
/// identities. Unset means every set. An unknown name is refused, naming the supported ones.
/// </summary>
public static class EvalSetSelection
{
    public static IReadOnlyList<EvalSetKind> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Enum.GetValues<EvalSetKind>();

        var kinds = new List<EvalSetKind>();
        foreach (string name in value.Split(',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!SettingChoice.TryParse(name, out EvalSetKind? kind) || kind is null)
                throw new RunnerException(MessageText.Format(RunnerMessage.UnknownEvalSet,
                    new { Name = name, Supported = string.Join(", ", SettingChoice.Names<EvalSetKind>()) }));
            if (!kinds.Contains(kind.Value)) kinds.Add(kind.Value);
        }

        return kinds;
    }
}
