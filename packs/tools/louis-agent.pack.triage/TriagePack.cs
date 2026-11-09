using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using LouisAgent.Packs.Triage.Departments;
using LouisAgent.Packs.Triage.Runs;
using LouisAgent.Packs.Triage.Tools;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Triage;

/// <summary>
/// What an agent team triages a service's errors with (F106.3.1): one toolset, <c>triage</c>, with the roles' typed
/// endings (an investigation, a review, a decision), a case raised to a person, and a run read back. Without its two
/// settings it stays off. The feed and the departments are the workflow's, not the model's, so they are no tool.
/// </summary>
/// <remarks>
/// The same shape as every pack (ADR 0027): a list of toolsets and the files the model reads, naming no identity and
/// no other pack. It comes after the engine's tools, as a product's pack does. Its tools write, so the model's calls
/// run one at a time.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0028-packs-stay-convertible-to-mcp.md">ADR 0028: packs stay convertible to MCP</seealso>
public sealed class TriagePack(TriageOptions options) : IAgentPack
{
    /// <inheritdoc />
    public string Name => TriageToolset.Triage.Wire();

    /// <inheritdoc />
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(TriageToolset.Triage.Wire(),
            "End a triage role's work typed, raise a case to a person, read a run back",
            GuideFile: PackToolset.Guide(TriageToolset.Triage.Wire()),
            MissingSettings: _ => Missing(options),
            ToolTypes: [typeof(TriageTools), typeof(RunTools)],
            CreateTools: ctx => Tools(options, () => ctx.InATrial)),
    ];

    /// <inheritdoc />
    public IFileProvider Prompts { get; } = new ManifestEmbeddedFileProvider(typeof(TriagePack).Assembly, "Prompts");

    /// <inheritdoc />
    public IReadOnlyList<string> Requires => [];

    /// <summary>The settings still unset, by name.</summary>
    private static IReadOnlyList<string> Missing(TriageOptions options) =>
    [
        .. options.Runs is null ? [TriageOptions.RunsSetting] : Array.Empty<string>(),
        .. options.People is null ? [TriageOptions.PeopleSetting] : Array.Empty<string>(),
        .. options.Departments is null ? [TriageOptions.DepartmentsSetting] : Array.Empty<string>(),
    ];

    /// <summary>
    /// Both tool classes over one recorder, on the system clock, with the departments' names for the sorter, and
    /// whether this turn is an eval's trial, read on each call.
    /// </summary>
    private static IReadOnlyList<object> Tools(TriageOptions options, Func<bool> inATrial)
    {
        var recorder = new FileTriageRecorder(options.Runs!);
        var departments = DepartmentsFile.Read(options.Departments!).Select(d => d.Name).ToList();
        return
        [
            new TriageTools(recorder, departments, TimeProvider.System, inATrial),
            new RunTools(recorder, options.People!, TimeProvider.System)
        ];
    }
}
