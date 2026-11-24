using LouisAgent.Core.Skills.Headers;

namespace LouisAgent.Identities.Triage.Tests;

/// <summary>
/// FX240: each voice of the triage team declares the toolsets it always loads in its header (ADR 0025), so a session
/// opened as the manager, or as one of its roles, has its tools whoever opens it. Before, an eval's session opened as
/// the manager had only the instance's default, and the manager could neither ask a role nor end a case (Louis's pick,
/// "Each voice declares its toolsets").
/// </summary>
public class TriageToolsetsTests
{
    /// <summary>A voice's header, as the engine reads it.</summary>
    private static PromptHeader HeaderOf(string path)
    {
        var file = new TriageIdentity().Prompts.GetFileInfo(path);
        Assert.That(file.Exists, Is.True, path);
        using var reader = new StreamReader(file.CreateReadStream());
        return PromptHeaders.Split(path, reader.ReadToEnd()).Header!;
    }

    /// <summary>The manager loads the team's tools and the toolset that asks; each role what its prose says.</summary>
    [TestCase("agents/triage/voice.md", "triage, agents")]
    [TestCase("agents/triage/subagents/sorter/voice.md", "triage")]
    [TestCase("agents/triage/subagents/investigator/voice.md", "triage, files, git, dotnet")]
    [TestCase("agents/triage/subagents/reviewer/voice.md", "triage, files, git")]
    public void EachVoice_DeclaresTheToolsetsItAlwaysLoads(string path, string toolsets) =>
        Assert.That(string.Join(", ", HeaderOf(path).Toolsets), Is.EqualTo(toolsets));

    /// <summary>
    /// The manager may ask its three roles and no one else, the coder never: a peer list that names this host lets it
    /// ask, and its calls keep an eval's auto-fix at the decision (Louis's pick, "Stop at the decision").
    /// </summary>
    [Test]
    public void TheManagersCalls_AreItsRoles_NeverTheCoder() =>
        Assert.That(new TriageIdentity().Calls,
            Is.EquivalentTo(new[] { "triage.sorter", "triage.investigator", "triage.reviewer" }));

    /// <summary>
    /// FX244: the roles that read the department's repository search inside it, passing it as the search's folder,
    /// not the whole workspace, so their results are the service's own lines.
    /// </summary>
    [TestCase("agents/triage/subagents/investigator/voice.md")]
    [TestCase("agents/triage/subagents/reviewer/voice.md")]
    public void ARoleThatReadsTheRepository_SearchesInsideIt(string path)
    {
        var file = new TriageIdentity().Prompts.GetFileInfo(path);
        using var reader = new StreamReader(file.CreateReadStream());

        Assert.That(reader.ReadToEnd(), Does.Contain("`searchPath`"));
    }
}
