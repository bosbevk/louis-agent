
using System.ComponentModel;

namespace LouisAgent.Core.Tools.DotNet.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the .NET toolset, returned to the model: what went wrong and what to do instead.</summary>
public enum DotNetToolMessage
{
    [Description("Error: specify the project to run (see DotNetListProjects).")]
    DotNetProjectRequired,

    [Description("Error: filter must be '', 'outdated' or 'vulnerable'.")]
    DotNetPackageFilterInvalid,

    [Description("Error: '{Project}' not found. Use DotNetListProjects to see available projects.")]
    DotNetProjectNotFound,

    [Description("No .NET solutions or projects found.")]
    NoDotNetProjects,

    [Description("Timed out.\n{Output}")] TimedOutWithOutput,

    [Description("Succeeded.")] BuildSucceeded,

    [Description("FAILED (exit code {ExitCode}).")]
    BuildFailed,

    [Description("... {Count} more warning(s)")]
    MoreWarnings,

    [Description("Test run timed out.\n{Output}")]
    TestRunTimedOut,

    [Description("... more failures omitted")]
    MoreFailures,

    [Description("Tests passed.")] TestsPassed,

    [Description("Build failed before tests ran.\n{Build}")]
    BuildFailedBeforeTests,
}
