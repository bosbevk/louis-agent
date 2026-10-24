using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Protocol;

namespace LouisAgent.Evals.Conformance;

/// <summary>
/// The conformance cases an eval keeps beside it (F18 step 4.2b): for each scenario, <c>{scenario}.reference.txt</c> (one conversation's
/// protocol lines, its mode line first) and <c>{scenario}.bad.{label}.txt</c> (the same shape, wrong in one way). A scenario with no
/// reference file has no case, and the check fails it.
/// </summary>
public static class ConformanceCases
{
    private const string ReferenceSuffix = ".reference.txt";
    private const string BadSuffix = ".txt";

    public static IReadOnlyList<ConformanceCase> Load(string folder, IReadOnlyList<EvalScenario> scenarios)
    {
        var cases = new List<ConformanceCase>();
        if (!Directory.Exists(folder)) return cases;

        var names = Directory.GetFiles(folder).Select(Path.GetFileName).OfType<string>().OrderBy(n => n, StringComparer.Ordinal).ToList();
        foreach (EvalScenario scenario in scenarios)
        {
            string reference = scenario.Id + ReferenceSuffix;
            if (!names.Contains(reference, StringComparer.Ordinal)) continue;

            var referenceLines = File.ReadAllLines(Path.Combine(folder, reference));
            string prefix = scenario.Id + ".bad.";
            var knownBad = names
                .Where(name => name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(BadSuffix, StringComparison.Ordinal))
                .Select(name => new KnownBad(name[prefix.Length..^BadSuffix.Length],
                    ProtocolTranscripts.ReadConversation(File.ReadAllLines(Path.Combine(folder, name)))))
                .ToList();
            cases.Add(new ConformanceCase(scenario.Id, ProtocolTranscripts.ReadMode(referenceLines),
                ProtocolTranscripts.ReadConversation(referenceLines), knownBad));
        }
        return cases;
    }
}
