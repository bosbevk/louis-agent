
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Git.Messages;

namespace LouisAgent.Core.Tools.Git.Running;
/// <summary>
/// The FX19 rules a value from the model passes before git runs (S, CL15, from GitTools): our own options come first,
/// then <see cref="EndOfOptions"/>, then the model's values, with paths after <c>--</c>; and a value that names
/// something and starts with <c>-</c> is refused, so a command that takes neither marker is covered too. Free text that
/// is an option's own argument (a <c>-m</c> message, a config value) travels whole, whatever it starts with.
/// </summary>
internal static class GitArguments
{
    public const string EndOfOptions = "--end-of-options";

    /// <summary>
    /// The refusal for a value that names something and starts with <c>-</c>: git would read it as an option, so it
    /// never runs. Null when the value is fine. Not for free text that is an option's own argument (<c>-m</c>).
    /// </summary>
    public static string? Refused(string parameter, string value) =>
        value.TrimStart().StartsWith('-')
            ? MessageText.Format(GitToolMessage.GitValueRefused, new { Parameter = parameter, Value = value })
            : null;

    /// <summary>
    /// The refusal for a branch that is really a refspec (review of PR #66): neither marker sees one, yet <c>+main</c>
    /// forces the push past <c>force: false</c>, <c>:main</c> deletes the remote's branch and <c>a:b</c> pushes to another
    /// ref. A branch name alone, or nothing, passes; a leading <c>-</c> is refused as elsewhere.
    /// </summary>
    public static string? RefusedRefspec(string parameter, string branch) =>
        branch.TrimStart().StartsWith('+') || branch.Contains(':')
            ? MessageText.Format(GitToolMessage.GitRefspecRefused, new { Parameter = parameter, Value = branch })
            : Refused(parameter, branch);
}
