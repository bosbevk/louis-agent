namespace LouisAgent.Web.Tests.Rules;

/// <summary>
/// The framework's web names no site: not in its code, markup, styles, scripts or comments. What differs from site to
/// site comes in from the site, so a new site never has to read past another's name.
/// </summary>
[TestFixture]
public class FrameworkNamesNoSiteTests
{
    private static readonly string[] Kinds = [".cs", ".razor", ".scss", ".css", ".js", ".html", ".csproj"];

    /// <summary>Every file in the framework's web, and in the account library on it (CL30), is read, and no line may hold a site's name.</summary>
    [TestCase("louis-agent.web")]
    [TestCase("louis-agent.web.accounts")]
    public void TheFramework_NamesNoSite(string library)
    {
        string[] names = [.. FrameworkWebSites.Folders().Select(FrameworkWebSites.NameOf).SelectMany(Spellings)];
        string root = Path.Combine(TestPaths.RepoRoot, "src/web", library);

        var found = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(file => Kinds.Contains(Path.GetExtension(file)) && !IsBuildOutput(file))
            .SelectMany(file => File.ReadLines(file).Select((line, index) => (file, line, index)))
            .Where(hit => names.Any(name => Names(hit.line, name)))
            .Select(hit => $"{Path.GetRelativePath(root, hit.file).Replace('\\', '/')}:{hit.index + 1}")
            .ToList();

        Assert.That(names, Is.Not.Empty, "No site was found on disk, so this test would prove nothing.");
        Assert.That(found, Is.Empty, "The framework's web names no site; pass it in from the site:\n  " +
                                     string.Join("\n  ", found));
    }

    /// <summary>The word stands alone or starts a longer name, so "acmeUi" counts and "acmes" doesn't.</summary>
    [TestCase("Acme's sidebar", "acme", true)]
    [TestCase("the role acme-cook", "acme", true)]
    [TestCase("acmeUi.focus", "acme", true)]
    [TestCase("two acmes of light", "acme", false)]
    [TestCase("a pacme", "acme", false)]
    [TestCase("the two-word app", "two-word", true)]
    public void AName_IsFound_OnlyAsAWord(string line, string name, bool expected) =>
        Assert.That(Names(line, name), Is.EqualTo(expected));

    /// <summary>A name with a dash is also written with a space, as in "code review".</summary>
    private static IEnumerable<string> Spellings(string name) =>
        name.Contains('-') ? [name, name.Replace('-', ' ')] : [name];

    /// <summary>Looks for the name as a word: no letter before it, and no small letter straight after it.</summary>
    private static bool Names(string line, string name)
    {
        int at = line.IndexOf(name, StringComparison.OrdinalIgnoreCase);
        while (at >= 0)
        {
            int end = at + name.Length;
            bool startsWord = at == 0 || !char.IsLetter(line[at - 1]);
            bool endsWord = end == line.Length || !char.IsLower(line[end]);
            if (startsWord && endsWord) return true;

            at = line.IndexOf(name, at + 1, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>What the compiler writes isn't ours to check.</summary>
    private static bool IsBuildOutput(string file)
    {
        string path = file.Replace('\\', '/');
        return path.Contains("/bin/") || path.Contains("/obj/");
    }
}
