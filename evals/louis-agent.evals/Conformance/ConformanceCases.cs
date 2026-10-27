using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol;

namespace LouisAgent.Evals.Conformance;

/// <summary>
/// The conformance cases an eval keeps beside it (F18 step 4.2b): for each scenario, <c>{scenario}.reference.txt</c> (one conversation's
/// protocol lines, its mode line first) and <c>{scenario}.bad.{label}.txt</c> (the same shape, wrong in one way). A scenario with no
/// reference file has no case, and the check fails it. A file that is neither, for one of the eval's scenarios, is stray and refused:
/// a misspelling, a deleted scenario's files, or a known-bad with no reference.
/// </summary>
public static class ConformanceCases
{
    private const string ReferenceSuffix = ".reference.txt";
    private const string BadSuffix = ".txt";

    public static IReadOnlyList<ConformanceCase> Load(string folder, IReadOnlyList<EvalScenario> scenarios)
    {
        var cases = new List<ConformanceCase>();
        if (!Directory.Exists(folder)) return cases;

        var names = Directory.GetFiles(folder).Select(Path.GetFileName).OfType<string>()
            .OrderBy(n => n, StringComparer.Ordinal).ToList();
        foreach (string name in names)
            if (!scenarios.Any(scenario => IsOf(name, scenario.Id)))
                throw Stray(name);

        foreach (EvalScenario scenario in scenarios)
        {
            var knownBadNames = names.Where(name => IsKnownBadOf(name, scenario.Id)).ToList();
            string reference = scenario.Id + ReferenceSuffix;
            if (!names.Contains(reference, StringComparer.Ordinal))
            {
                if (knownBadNames.Count > 0) throw Stray(knownBadNames[0]);
                continue;
            }

            var referenceLines = File.ReadAllLines(Path.Combine(folder, reference));
            string prefix = scenario.Id + ".bad.";
            var knownBad = knownBadNames
                .Select(name => new KnownBad(name[prefix.Length..^BadSuffix.Length],
                    ProtocolTranscripts.ReadConversation(File.ReadAllLines(Path.Combine(folder, name)))))
                .ToList();
            cases.Add(new ConformanceCase(scenario.Id, ProtocolTranscripts.ReadMode(referenceLines),
                ProtocolTranscripts.ReadConversation(referenceLines), knownBad));
        }

        return cases;
    }

    /// <summary>Whether a file belongs to a scenario: its reference, or one of its known-bad transcripts.</summary>
    private static bool IsOf(string name, string scenario) =>
        name == scenario + ReferenceSuffix || IsKnownBadOf(name, scenario);

    /// <summary>Whether a file is a known-bad of a scenario: <c>{scenario}.bad.{label}.txt</c>, with a label.</summary>
    private static bool IsKnownBadOf(string name, string scenario)
    {
        string prefix = scenario + ".bad.";
        return name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(BadSuffix, StringComparison.Ordinal)
                                                                 && name.Length > prefix.Length + BadSuffix.Length;
    }

    private static InvalidDataException Stray(string name) =>
        new(MessageText.Format(EvalErrorMessage.StrayTranscript, new { File = name }));
}
