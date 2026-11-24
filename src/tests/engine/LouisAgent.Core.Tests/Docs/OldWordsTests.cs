namespace LouisAgent.Core.Tests.Docs;

/// <summary>
/// The levels below a voice are a subagent and a lookup agent (ADR 0133, ADR 0021), so outside the places kept on
/// purpose no file says "specialist" or "minion", and no text the model or a person reads ever does (CL69).
/// </summary>
/// <remarks>
/// It was a ratchet while CL69 ran, as <see cref="WriteUpsTests"/> is: the files that still said the words were
/// listed, and each step took its own off. The list ended empty, so the rule now holds every file; the texts were
/// held from the first day, since a word the model reads steers what it does.
/// </remarks>
public class OldWordsTests
{
    /// <summary>The words the levels were called before CL68.2 and CL68.3.</summary>
    private static readonly string[] OldWords = ["minion", "specialist"];

    /// <summary>Where our own source lives; the docs, in each <c>docs/</c> folder, have their own reviews.</summary>
    private static readonly string[] SourceRoots = ["src", "packs", "demos"];

    /// <summary>What we write by hand and a model or a person may read: code, markup, styles, data.</summary>
    private static readonly string[] SourceExtensions = [".cs", ".razor", ".scss", ".js", ".json", ".csproj"];

    /// <summary>
    /// The places that keep the words on purpose: Louis's own CV, which says what the Puls specialists are; the coding
    /// agent's persona, whose name is his, with the golden file that pins its prompt; the guide's eval recordings,
    /// which keep what its docs tools read, history rows included; and the tests whose job is the words: this one,
    /// the framework pages' check, and the headers' check that the old levels are refused.
    /// </summary>
    private static readonly string[] Kept =
    [
        "src/site/LouisAgent.Site/Content/cv.json",
        "demos/louis-agent/identity/LouisAgent.Identities.Coding/agents/louis-agent/voice.md",
        "src/tests/engine/LouisAgent.Core.Tests/Docs/OldWordsTests.cs",
        "src/tests/web/LouisAgent.Web.Framework.Tests/SubagentWordTests.cs",
        "src/tests/engine/LouisAgent.Core.Tests/Skills/Headers/PromptHeadersTests.cs",
        "demos/louis-agent/tests/LouisAgent.Coding.Catalog.Tests/Golden/ModelRequestSnapshot.json",
        "src/site/recordings/evals/",
    ];

    /// <summary>No <c>[Description]</c> says the old words: what a tool, a message or a setting tells.</summary>
    [Test]
    public void NoText_SaysTheOldWords()
    {
        var saying = SourceFiles()
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal))
            .SelectMany(file => Descriptions(Read(file)).Where(SaysAnOldWord).Select(text => $"{file}: {text}"))
            .ToList();

        Assert.That(saying, Is.Empty,
            "A text says subagent or lookup agent, never specialist or minion (ADR 0133, ADR 0021):\n  " +
            string.Join("\n  ", saying));
    }

    /// <summary>No file outside the kept places says the words.</summary>
    [Test]
    public void NoFile_SaysTheOldWords()
    {
        var saying = SourceFiles().Where(file => SaysAnOldWord(Read(file))).Order().ToList();

        Assert.That(saying, Is.Empty,
            "Say subagent or lookup agent (ADR 0133, ADR 0021); these say specialist or minion:\n  " +
            string.Join("\n  ", saying));
    }

    /// <summary>A kept place keeps its words, and a file beside one is still read.</summary>
    [TestCase("demos/louis-agent/identity/LouisAgent.Identities.Coding/agents/louis-agent/voice.md", true)]
    [TestCase("src/evals/LouisAgent.Evals/Protocol/ProtocolWriter.cs", false)]
    [TestCase("src/site/LouisAgent.Site/Content/cv.json", true)]
    [TestCase("src/hosts/LouisAgent.Api.Contracts/Admin/Prompts/PromptRows.cs", false)]
    [TestCase("src/web/LouisAgent.Web/Chats/Delegation.cs", false)]
    public void AKeptPlace_IsSkipped(string file, bool kept) => Assert.That(IsKept(file), Is.EqualTo(kept));

    /// <summary>Louis's own words keep what he said; the same word outside his quote is still found.</summary>
    [TestCase("(Louis: \"the specialist must give him its tools\")", false)]
    [TestCase("Louis, 2026-11-22: \"a minion\", and a minion", true)]
    [TestCase("Louis's pick; the specialist loads", true)]
    public void AQuoteOfLouiss_KeepsHisWords(string text, bool says) =>
        Assert.That(SaysAnOldWord(text), Is.EqualTo(says));

    /// <summary>A description is read whole, even when its text is split across lines with <c>+</c>.</summary>
    [Test]
    public void ADescription_IsReadAcrossItsLines()
    {
        const string source = "[Description(\"one \" +\n    \"two\")]\nFirst,\n[Description(\"three\")] Second";

        Assert.That(Descriptions(source), Is.EqualTo(new[] { "\"one \" +\n    \"two\"", "\"three\"" }));
    }

    /// <summary>
    /// Every source file under the roots, as a path from the repository's root with forward slashes: not what a build
    /// wrote, not a doc, not a kept place. A prompt is a markdown file under an <c>agents</c> folder.
    /// </summary>
    private static IEnumerable<string> SourceFiles() =>
        SourceRoots
            .Select(root => Path.Combine(TestPaths.RepoRoot, root))
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/'))
            .Where(IsOurs)
            .Where(file => !IsKept(file));

    /// <summary>
    /// A file we wrote by hand, in a kind we read: never build output, a package's files, a host's own data or a doc.
    /// </summary>
    private static bool IsOurs(string file)
    {
        if (file.Contains("/bin/") || file.Contains("/obj/") || file.Contains("/node_modules/")) return false;
        // What a host keeps as it runs (git-ignored), such as an eval's runs quoting the docs it read.
        if (file.StartsWith("src/site/data/", StringComparison.Ordinal)) return false;
        if (file.StartsWith("demos/", StringComparison.Ordinal) && file.Contains("/data/")) return false;
        if (file.Contains("/docs/")) return false;
        if (file.EndsWith(".md", StringComparison.Ordinal)) return file.Contains("/agents/");
        return SourceExtensions.Any(extension => file.EndsWith(extension, StringComparison.Ordinal));
    }

    /// <summary>Whether a file is in a place that keeps the old words on purpose.</summary>
    private static bool IsKept(string file) => Kept.Any(place => file.Contains(place, StringComparison.Ordinal));

    /// <summary>
    /// Whether a text says either old word, in any case or form ("Minions", "specialist's"), outside Louis's own
    /// words: a quote of his keeps what he said.
    /// </summary>
    private static bool SaysAnOldWord(string text)
    {
        string ours = NamesThatStay.Aggregate(WithoutLouissQuotes(text),
            (rest, gone) => rest.Replace(gone, "", StringComparison.Ordinal));
        return OldWords.Any(word => ours.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Names that keep an old word and are no word anyone reads: <c>MINION_LLMS</c>, which a test writes to prove it
    /// sets nothing since CL68.3 (ADR 0049); the tools <c>StartMinion</c> and <c>ContinueMinion</c> and the toolset
    /// <c>`minions`</c>, which tests prove are gone; and ADR 0021's file name, which links to it.
    /// </summary>
    private static readonly string[] NamesThatStay =
        ["MINION_LLMS", "StartMinion", "ContinueMinion", "`minions`", "0021-minions-one-task-one-tool.md"];

    /// <summary>
    /// The text with each quote of Louis's taken out: from <c>Louis</c> to the next opening quote within a few words
    /// (<c>Louis, 2026-11-22: "</c>), then to its closing quote.
    /// </summary>
    private static string WithoutLouissQuotes(string text)
    {
        const int reach = 30;
        var kept = new System.Text.StringBuilder();
        int from = 0;
        int at = text.IndexOf("Louis", StringComparison.Ordinal);
        while (at >= 0)
        {
            int open = text.IndexOf('"', at);
            int close = open < 0 || open - at > reach ? -1 : text.IndexOf('"', open + 1);
            if (close < 0)
            {
                at = text.IndexOf("Louis", at + 1, StringComparison.Ordinal);
                continue;
            }

            kept.Append(text, from, open - from);
            from = close + 1;
            at = text.IndexOf("Louis", from, StringComparison.Ordinal);
        }

        return kept.Append(text, from, text.Length - from).ToString();
    }

    /// <summary>The file's text, from its path from the repository's root.</summary>
    private static string Read(string file) => File.ReadAllText(Path.Combine(TestPaths.RepoRoot, file));

    /// <summary>
    /// The text of each <c>[Description(...)]</c> in a source file, from after its opening parenthesis to its closing
    /// <c>)]</c>, so a text split over lines is read whole.
    /// </summary>
    private static IEnumerable<string> Descriptions(string source)
    {
        const string open = "[Description(";
        const string close = ")]";
        int at = source.IndexOf(open, StringComparison.Ordinal);
        while (at >= 0)
        {
            int start = at + open.Length;
            int end = source.IndexOf(close, start, StringComparison.Ordinal);
            if (end < 0) yield break;
            yield return source[start..end];
            at = source.IndexOf(open, end, StringComparison.Ordinal);
        }
    }
}
