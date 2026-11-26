using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Messages.Holes.Errors;
using LouisAgent.Composition.Messages.Holes.Processes;
using LouisAgent.Composition.Tools;
using LouisAgent.Composition.Tools.Scripts;
using LouisAgent.Core.Prompting.Languages;
using LouisAgent.Core.Prompting.Messages;
using LouisAgent.Core.Prompting.Messages.Holes.Running;

namespace LouisAgent.Core.Prompting.Headers.Agent.Running;

/// <summary>
/// Runs a skill's script with a call's arguments as JSON on stdin (F97.1), and gives back what it printed, or why it
/// failed, with whether it succeeded: a draft is kept only when its example's run did.
/// </summary>
/// <remarks>
/// The arguments go only to stdin, never into the script's command line or its environment, so nothing the model sends
/// can change what runs (ADR 0004, arguments as data). The script is run through the runners core already has, behind
/// one <see cref="ScriptRun"/> (a Strategy as a delegate), so the answer's rules are tested without starting a process.
/// </remarks>
/// <seealso href="https://agentskills.io/skill-creation/using-scripts">Agent Skills: using scripts</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public sealed class SkillRunner(ScriptRun run)
{
    /// <summary>How long a skill's script may run, as long as an agent-built tool's could.</summary>
    public static readonly TimeSpan TimeLimit = TimeSpan.FromSeconds(60);

    /// <summary>A runner over core's own script runners, each script run from the workspace.</summary>
    public static SkillRunner For(PythonRunner python, PowerShellRunner powerShell, BashRunner bash) =>
        new((ScriptLanguage language, string script, string input, TimeSpan limit, out string? notInstalled) =>
            language switch
            {
                ScriptLanguage.Python => python.RunScript(null, script, [], input, limit, out notInstalled),
                ScriptLanguage.PowerShell => powerShell.RunScript(null, script, [], input, limit, out notInstalled),
                ScriptLanguage.Bash => bash.RunScript(null, script, [], input, limit, strict: false, out notInstalled),
                _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
            });

    /// <summary>What the skill's script answered to these arguments.</summary>
    public SkillAnswer Run(Skill skill, JsonElement arguments)
    {
        var result = run(skill.Language, skill.Script, arguments.GetRawText(), TimeLimit, out string? notInstalled);
        if (notInstalled is not null || result is null)
        {
            var missing = new ErrorWithReasonHoles(notInstalled ?? "");
            return new SkillAnswer(false, MessageText.Format(ToolMessage.ErrorWithReason, missing));
        }

        if (result.TimedOut)
        {
            var timedOut = new SkillTimedOutHoles(skill.Name, (int)TimeLimit.TotalSeconds,
                ProcessRunner.Tail(result.Output));
            return new SkillAnswer(false, MessageText.Format(SkillToolMessage.SkillTimedOut, timedOut));
        }

        string output = ProcessRunner.Truncate(result.Output.Trim());
        if (result.ExitCode != 0)
        {
            var failed = new ExitCodeHoles(result.ExitCode, output);
            return new SkillAnswer(false, MessageText.Format(ToolMessage.ExitCode, failed));
        }

        if (output.Length > 0) return new SkillAnswer(true, output);

        var silent = new SkillRanNoOutputHoles(skill.Name);
        return new SkillAnswer(true, MessageText.Format(SkillToolMessage.SkillRanNoOutput, silent));
    }
}
