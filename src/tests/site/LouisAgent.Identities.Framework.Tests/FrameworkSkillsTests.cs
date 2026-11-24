using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;
using LouisAgent.Core.Skills;

namespace LouisAgent.Identities.Framework.Tests;

/// <summary>
/// The framework Orbi's prompts take the shape the public Orbi's do (CL64): its voice holds the rules it never breaks
/// and a table of the skills it reads for each job, and every skill opens with its Never, one to seven rules, then
/// shows an example and names its references.
/// </summary>
public class FrameworkSkillsTests
{
    /// <summary>Where the skills are among the identity's prompts.</summary>
    private const string SkillsFolder = "agents/louis-agent-framework/skills";

    /// <summary>The most rules a skill's Never holds.</summary>
    private const int MostNevers = 7;

    /// <summary>A prompt's text, its line breaks made one kind, so a checkout on any machine reads the same.</summary>
    private static string Read(string path)
    {
        using var stream = new FrameworkIdentity().Prompts.GetFileInfo($"agents/louis-agent-framework/{path}")
            .CreateReadStream();
        return new StreamReader(stream).ReadToEnd().ReplaceLineEndings("\n");
    }

    /// <summary>The skills' names: a folder each, holding its <c>SKILL.md</c> (CL68.1).</summary>
    private static IEnumerable<string> Skills() =>
        new FrameworkIdentity().Prompts.GetDirectoryContents(SkillsFolder).Select(folder => folder.Name);

    /// <summary>One skill's text, from the <c>SKILL.md</c> in its folder.</summary>
    private static string Skill(string name) => Read($"skills/{name}/{PromptFileName.Skill.Wire()}");

    /// <summary>How many numbered rules stand under a section, up to the next one.</summary>
    private static int Rules(string text, int section)
    {
        int next = text.IndexOf("\n## ", section + 1, StringComparison.Ordinal);
        string rules = next < 0 ? text[section..] : text[section..next];
        return rules.Split('\n').Count(line => line.Length > 2 && char.IsDigit(line[0]) && line[1] == '.');
    }

    /// <summary>Every skill: its Never first, one to seven rules, then its example, then its references.</summary>
    [Test]
    public void EverySkill_HasTheSkillShape()
    {
        var wrong = Skills().Where(name =>
        {
            string text = Skill(name);
            int never = text.IndexOf("\n## Never\n", StringComparison.Ordinal);
            int example = text.IndexOf("\n## An example\n", StringComparison.Ordinal);
            int references = text.IndexOf("\n## References\n", StringComparison.Ordinal);
            return never < 0 || never > example || example > references || Rules(text, never) is 0 or > MostNevers;
        }).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(Skills().Count(), Is.GreaterThanOrEqualTo(4), "the skills were found");
            Assert.That(wrong, Is.Empty, "Never first (1 to 7 rules), then the example, then the references");
        });
    }

    /// <summary>The voice holds its rules once, at most seven, and a table of every on-demand skill it reads.</summary>
    [Test]
    public void TheVoice_HoldsItsNever_AndATableOfItsSkills()
    {
        string voice = Read("voice.md");
        int never = voice.IndexOf("\n## Never\n", StringComparison.Ordinal);
        int table = voice.IndexOf("\n## Your skills\n", StringComparison.Ordinal);
        var onDemand = Skills().Where(name => Skill(name).Contains("load: on-demand"))
            .Select(name => $"`louis-agent-framework/{name}`");

        Assert.Multiple(() =>
        {
            Assert.That(never, Is.GreaterThan(0), "the voice has its Never");
            Assert.That(never < 0 ? 0 : Rules(voice, never), Is.InRange(1, MostNevers));
            Assert.That(table, Is.GreaterThan(never), "its skills' table after its Never");
            Assert.That(onDemand.Where(skill => !voice.Contains(skill)), Is.Empty,
                "every on-demand skill in the table");
        });
    }

    /// <summary>
    /// F126.5.3: the pages Orbi opens tell the evals as they are built, so its map of them does too, never as being
    /// rebuilt or redesigned: one file of conversations each, the guide's, Orbi's own and the triage team's.
    /// </summary>
    [Test]
    public void TheSitePages_TellTheEvalsAsTheyAre()
    {
        string text = Skill("site-pages");

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Not.Contain("rebuilt").And.Not.Contain("redesign"));
            Assert.That(text, Does.Contain("one file of conversations").And.Contain("one page per run"));
        });
    }

    /// <summary>
    /// FX236: under the code, every setting the program needs, one line each, not one of them; Orbi's baseline named
    /// only the model key.
    /// </summary>
    [Test]
    public void UnderTheCode_ItNamesEverySettingItNeeds() =>
        Assert.That(Read("voice.md"),
            Does.Contain("every setting it needs to run").And.Contain("a second fenced block"));

    /// <summary>
    /// FX237: never an answer without words, a page shown or a choice card alone; once in three Orbi answered a
    /// walkthrough so, as the guide did (FX233).
    /// </summary>
    [Test]
    public void AnAnswer_AlwaysHasWords_BeforeAnyPageOrCard() =>
        Assert.That(Skill("writing"), Does.Contain("Never an answer without words"));

    /// <summary>
    /// FX238: the doc's path is the last line of the words, and a listing's summary one sentence: Orbi wrote past the
    /// path, and once summed a listing up in two.
    /// </summary>
    [Test]
    public void TheDocsPath_EndsTheWords_AndAListingIsSummedUpInOneSentence()
    {
        Assert.That(Read("voice.md"), Does.Contain("the last line of your words"));
        Assert.That(Skill("explain-code"), Does.Contain("one sentence, never two"));
    }
}
