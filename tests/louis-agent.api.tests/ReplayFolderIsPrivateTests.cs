using System.Text.RegularExpressions;

namespace LouisAgent.Api.Tests;

/// <summary>
/// The saved web pages are private (F18 step 4a: "no endpoint serves the folder"): only the store opens <c>REPLAY_DIRECTORY</c>, so
/// nothing can serve it. A source test, as <c>PortalReadsNoScoreKeyTests</c> is: outside the setting's own class and the store's factory method, no .cs or .razor file
/// under core, the hosts, the identities or the packs names it. What the API needs to know is <c>AgentOptions.KeepsRecordings</c>, not where. A scanned folder that isn't there fails, rather than passing empty.
/// </summary>
public class ReplayFolderIsPrivateTests
{
    private static readonly Regex Names = new("ReplayDirectory|REPLAY_DIRECTORY", RegexOptions.CultureInvariant);

    private static readonly string[] Scanned = ["louis-agent.core", "src", "identities", "packs"];

    private static readonly string[] MayName =
    [
        "louis-agent.core/Config/AgentOptions.cs",
        "louis-agent.core/Tools/Web/Replay/Store/WebReplayStore.cs",
    ];

    [Test]
    public void OnlyTheSettingAndTheStoreNameTheReplayFolder()
    {
        var naming = Scanned.SelectMany(Files).Where(f => Names.IsMatch(File.ReadAllText(f))).Select(Relative).Except(MayName).Order().ToList();

        Assert.That(naming, Is.Empty,
            "Only the store opens the replay folder, so no endpoint can serve it (use AgentOptions.KeepsRecordings to ask whether one is kept):\n  "
            + string.Join("\n  ", naming));
    }

    [Test]
    public void TheTwoFilesThatMayNameIt_StillExist_AndDo() =>
        Assert.That(MayName.Select(f => File.Exists(Path.Combine(TestPaths.RepoRoot, f)) && Names.IsMatch(File.ReadAllText(Path.Combine(TestPaths.RepoRoot, f)))),
            Is.All.True, "the allowed list names a file that moved, or that no longer holds the setting: update it");

    private static IEnumerable<string> Files(string folder)
    {
        string dir = Path.Combine(TestPaths.RepoRoot, folder);
        Assert.That(Directory.Exists(dir), Is.True, $"No folder {folder}: it was moved or renamed, so update this test's list.");
        return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".cs") || f.EndsWith(".razor"))
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
    }

    private static string Relative(string file) => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/');
}
