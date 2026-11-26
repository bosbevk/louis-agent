using System.Text.Json;
using LouisAgent.Core.Prompting.Headers.Agent;
using LouisAgent.Core.Prompting.Headers.Agent.Library;
using LouisAgent.Core.Prompting.Languages;

namespace LouisAgent.Core.Tests.Prompting.Headers.Agent.Library;

/// <summary>
/// F97.1 step 7: the approved skills are shown to the model as one line each, the name it calls and the line that says
/// what it does and when, in the library's order; it reads the rest of a skill only when it needs it, as the Agent
/// Skills spec loads a skill's body on demand.
/// </summary>
public class SkillListingTests
{
    /// <summary>One line a skill, its name and its description, in the order given.</summary>
    [Test]
    public void EachSkill_IsOneLine_ItsNameAndDescription()
    {
        Skill[] approved = [Skill("char-count", "Counts characters."), Skill("word-count", "Counts words.")];

        var lines = SkillListing.LinesFor(approved);

        Assert.That(lines.Split('\n'), Is.EqualTo(new[]
        {
            "- char-count: Counts characters.",
            "- word-count: Counts words."
        }));
    }

    /// <summary>With no skills, one line says there are none yet, so the model doesn't look for one.</summary>
    [Test]
    public void NoSkills_SaysSo() =>
        Assert.That(SkillListing.LinesFor([]), Does.Contain("No skills"));

    /// <summary>An approved skill with this name and description.</summary>
    private static Skill Skill(string name, string description) => new(name, description, "", name, "run.py",
        ScriptLanguage.Python, new SkillParameters(JsonDocument.Parse("{}").RootElement,
            new Dictionary<string, SkillParameterType>(), []), SkillState.Approved);
}
