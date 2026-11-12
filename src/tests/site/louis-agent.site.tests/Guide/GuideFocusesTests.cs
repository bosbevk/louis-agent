using LouisAgent.Choices;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Identities.Guide;

namespace LouisAgent.Site.Tests.Guide;

/// <summary>
/// F127.7, Louis: one guide for the whole website, "when on normal pages it talk about selling the product... /demos
/// it is a demo guide... /framework our agent should be more technical"; his pick, a focus skill read when the focus
/// changes. Each part of the site has its skill, and the voice tells him when to read it.
/// </summary>
public class GuideFocusesTests
{
    private const string Folder = "agent/louis-agent-guide/";

    /// <summary>A prompt file of the guide's, split into its header and the body the model reads.</summary>
    private static PromptFile Read(string path)
    {
        using var reader = new StreamReader(new GuideIdentity().Prompts.GetFileInfo(Folder + path).CreateReadStream());
        return PromptHeaders.Split(Folder + path, reader.ReadToEnd());
    }

    /// <summary>A prompt's text on one line, so a rule can be found whatever its line breaks.</summary>
    private static string Flat(string text) =>
        string.Join(' ', text.Split((char[])[' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Every part of the site has a skill of its own, read on demand, under a heading.</summary>
    [TestCaseSource(nameof(Focuses))]
    public void EveryFocus_HasASkillReadOnDemand(GuideFocus focus)
    {
        var file = Read($"skills/focus-{focus.Wire()}.md");

        Assert.Multiple(() =>
        {
            Assert.That(file.Header?.Load, Is.EqualTo(SkillLoad.OnDemand));
            Assert.That(file.Header?.Name, Is.EqualTo($"focus-{focus.Wire()}"));
            Assert.That(file.Body, Does.StartWith("# "));
        });
    }

    /// <summary>The parts the guide knows.</summary>
    private static IEnumerable<GuideFocus> Focuses() => Enum.GetValues<GuideFocus>();

    /// <summary>
    /// The voice says when to read one: the page block names the part, and he reads its skill the first time it shows
    /// in a chat and whenever it changes, before he answers.
    /// </summary>
    [Test]
    public void TheVoice_ReadsTheFocusSkill_WhenThePartChanges()
    {
        string voice = Flat(Read("voice.md").Body);

        Assert.Multiple(() =>
        {
            Assert.That(voice, Does.Contain("[The part of the site: ...]"));
            Assert.That(voice, Does.Contain("read its skill with `ReadSkill`"));
            Assert.That(voice, Does.Contain("whenever the part changes, before you answer"));
            Assert.That(Enum.GetValues<GuideFocus>().Where(focus => !voice.Contains($"`focus-{focus.Wire()}`")),
                Is.Empty, "every focus skill is named");
        });
    }

    /// <summary>
    /// The business reader is the selling part's: the voice no longer assumes a business owner everywhere, so on
    /// /framework he can start technical; that default lives in the selling skill.
    /// </summary>
    [Test]
    public void TheBusinessReader_IsTheSellingPartsDefault()
    {
        string voice = Flat(Read("voice.md").Body);
        string selling = Flat(Read("skills/focus-selling.md").Body);
        string framework = Flat(Read("skills/focus-framework.md").Body);

        Assert.Multiple(() =>
        {
            Assert.That(voice, Does.Not.Contain("speak as if they run a business"));
            Assert.That(selling, Does.Contain("speak as if they run a business"));
            Assert.That(selling, Does.Contain("guide them to the demos").And.Contain("`/demos`"),
                "Louis: \"it must also guide them to demos\"");
            Assert.That(selling, Does.Contain("Give first").And.Contain("small yes").And.Contain("their own business")
                .And.Contain("never a false hurry"), "Louis's pick: honest hooks, never manipulation");
            Assert.That(framework, Does.Contain("diagrams").And.Contain("planned"));
        });
    }
}
