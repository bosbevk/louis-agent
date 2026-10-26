using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Messages;

namespace LouisAgent.Evals.Labels;

/// <summary>
/// Reads a dataset's <c>labels/*.json</c> (F18 step 6.3), strictly: every file is a <see cref="LabelFile"/>, labels a grader the eval has, a scenario the dataset has and a turn that
/// scenario has, and holds the person's verdict. A file with no verdict yet is refused with its name, so a half-labelled sheet is never calibrated against as if it were whole. The folder is optional:
/// a dataset with none has no labels. The dataset's hash covers the scenarios only, so labelling bumps no version.
/// </summary>
public static class LabelLoader
{
    private const string LabelsFolder = "labels";

    public static IReadOnlyList<LabeledAnswer> Load(string datasetFolder, IReadOnlyList<EvalScenario> scenarios, IReadOnlySet<string> graders)
    {
        string folder = Path.Combine(datasetFolder, LabelsFolder);
        if (!Directory.Exists(folder)) return [];

        var labels = new List<LabeledAnswer>();
        foreach (string path in Directory.GetFiles(folder, "*.json").Order(StringComparer.Ordinal))
            labels.Add(Read(path, scenarios, graders));
        return labels;
    }

    private static LabeledAnswer Read(string path, IReadOnlyList<EvalScenario> scenarios, IReadOnlySet<string> graders)
    {
        string name = Path.GetFileName(path);
        LabelFile file;
        try
        {
            file = JsonSerializer.Deserialize<LabelFile>(File.ReadAllText(path), LabelJson.Options)
                   ?? throw Refuse(EvalErrorMessage.InvalidLabel, new { File = name, Reason = MessageText.Format(EvalErrorMessage.EmptyFile) });
        }
        catch (JsonException error)
        {
            throw Refuse(EvalErrorMessage.InvalidLabel, new { File = name, Reason = error.Message });
        }

        if (file.Label?.Pass is not { } pass) throw Refuse(EvalErrorMessage.LabelNotGiven, new { File = name });
        if (!graders.Contains(file.Grader)) throw Refuse(EvalErrorMessage.LabelGraderUnknown, new { File = name, file.Grader, Graders = string.Join(", ", graders.Order(StringComparer.Ordinal)) });
        var scenario = scenarios.FirstOrDefault(s => s.Id == file.Scenario)
                       ?? throw Refuse(EvalErrorMessage.LabelScenarioUnknown, new { File = name, file.Scenario });
        if (file.Turn < 1 || file.Turn > scenario.Input.Messages.Count)
            throw Refuse(EvalErrorMessage.LabelTurnUnknown, new { File = name, file.Turn, file.Scenario });

        return new LabeledAnswer(name, file.Grader, file.Scenario, file.Mode, file.Run, file.Repeat, file.Turn, file.Answer, pass, file.Label.Note);
    }

    private static InvalidDataException Refuse(EvalErrorMessage message, object values) => new(MessageText.Format(message, values));
}
