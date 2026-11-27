using LouisAgent.Composition.Tools;
using LouisAgent.Core.Prompting.Languages;

namespace LouisAgent.Core.Prompting.Headers.Agent.Running;

/// <summary>
/// Runs a script file in its language with this text on stdin, within the time limit; no result, and the reason, when
/// the language isn't installed.
/// </summary>
public delegate ProcessRunner.Result? ScriptRun(ScriptLanguage language, string script, string input, TimeSpan limit,
    out string? notInstalled);
