using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Prompts;
using LouisAgent.Core.Prompting.Headers.Agent.Arguments;
using LouisAgent.Core.Prompting.Headers.Agent.Reading;
using LouisAgent.Core.Prompting.Headers.Agent.Running;
using LouisAgent.Core.Prompting.Messages;
using LouisAgent.Core.Prompting.Messages.Holes.Drafting;

namespace LouisAgent.Core.Prompting.Headers.Agent.Drafting;

/// <summary>
/// Makes a draft of a skill the agent wrote, once it is proved (F97.1): its files written to <c>drafts/{name}/</c> and
/// read back as a skill, its example checked against its parameters and run. Only a skill that passes all three is
/// kept, with its example and answer beside it; anything short of that leaves nothing on disk and says what to fix.
/// </summary>
/// <remarks>
/// Verify, then commit, as a self-improving agent's skill library keeps a skill only once a check confirms it works
/// (Voyager), and Anthropic's skill-creator tests a skill before it is done (Louis's pick, 2026-11-26). Guard clauses
/// (ADR 0051), each refusal a text the model reads. The name becomes a folder, so it is held to the Agent Skills
/// spec's shape before anything is written, and the script is written only inside the skill's own folder.
/// </remarks>
/// <seealso href="https://arxiv.org/abs/2305.16291">Voyager: an open-ended embodied agent with large language models</seealso>
/// <seealso href="https://agentskills.io/specification">Agent Skills: the specification</seealso>
public sealed class SkillDrafting(string skillsFolder, SkillRunner runner)
{
    /// <summary>The longest name the Agent Skills spec takes.</summary>
    private const int LongestName = 64;

    /// <summary>The skill drafted and proved, or why not.</summary>
    public SkillAnswer Draft(string name, string skillFile, string parameters, string script, JsonElement example)
    {
        if (!IsAName(name))
        {
            var invalid = new SkillNameInvalidHoles(name, LongestName);
            return Refused(MessageText.Format(SkillToolMessage.SkillNameInvalid, invalid));
        }

        if (Directory.Exists(Path.Combine(skillsFolder, SkillFiles.ApprovedSkill(name))))
        {
            var approved = new SkillAlreadyApprovedHoles(name);
            return Refused(MessageText.Format(SkillToolMessage.SkillAlreadyApproved, approved));
        }

        string folder = Path.Combine(skillsFolder, SkillFiles.DraftSkill(name));
        var answer = Prove(folder, name, skillFile, parameters, script, example);
        if (!answer.Succeeded && Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        return answer;
    }

    /// <summary>The files written, read back, and the example run; its answer either way.</summary>
    private SkillAnswer Prove(string folder, string name, string skillFile, string parameters, string script,
        JsonElement example)
    {
        // A draft of the same name is replaced whole, so nothing of the old one is left beside the new.
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, PromptFileName.Skill.Wire()), skillFile);
        File.WriteAllText(Path.Combine(folder, SkillFiles.ParametersFile), parameters);
        WriteScript(folder, skillFile, script);

        Skill skill;
        try
        {
            skill = SkillReader.Read(folder, SkillState.Drafted);
        }
        catch (SkillRefusedException refused)
        {
            return Refused(refused.Message);
        }

        if (SkillArguments.Problems(skill.Parameters, example) is { Count: > 0 } problems)
        {
            var misfit = new ExampleDoesNotFitHoles(name, string.Join(" ", problems));
            return Refused(MessageText.Format(SkillToolMessage.ExampleDoesNotFit, misfit));
        }

        var run = runner.Run(skill, example);
        if (!run.Succeeded)
        {
            var failed = new ExampleFailedHoles(name, run.Text);
            return Refused(MessageText.Format(SkillToolMessage.ExampleFailed, failed));
        }

        Keep(folder, example, run.Text);
        var drafted = new SkillDraftedHoles(name, run.Text);
        return new SkillAnswer(true, MessageText.Format(SkillToolMessage.SkillDrafted, drafted));
    }

    /// <summary>
    /// The script, at the path the header names, when the header reads and the path is inside the folder; otherwise
    /// nothing is written, and the reader says what is wrong.
    /// </summary>
    private static void WriteScript(string folder, string skillFile, string script)
    {
        string? named;
        try
        {
            named = PromptHeaders.Split(Path.Combine(folder, PromptFileName.Skill.Wire()), skillFile).Header?.Script;
        }
        catch (PromptHeaderException)
        {
            return;
        }

        if (named is null) return;

        string path = Path.GetFullPath(Path.Combine(folder, named));
        if (!path.StartsWith(Path.GetFullPath(folder) + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, script);
    }

    /// <summary>The example and its answer, beside the skill.</summary>
    private static void Keep(string folder, JsonElement example, string answer)
    {
        string path = Path.Combine(folder, SkillFiles.ExampleFile);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var kept = new SkillExample(example, answer);
        File.WriteAllText(path, JsonSerializer.Serialize(kept, SkillExample.Options));
    }

    /// <summary>A name the spec takes: lowercase letters, digits and hyphens, no hyphen at either end.</summary>
    private static bool IsAName(string name) =>
        name.Length is > 0 and <= LongestName &&
        name.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-') &&
        name[0] != '-' && name[^1] != '-';

    /// <summary>A draft that wasn't kept, and why.</summary>
    private static SkillAnswer Refused(string why) => new(false, why);
}
