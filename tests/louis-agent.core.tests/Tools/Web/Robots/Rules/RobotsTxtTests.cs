using LouisAgent.Core.Tools.Web.Robots.Rules;

namespace LouisAgent.Core.Tests.Tools.Web.Robots.Rules;

/// <summary>
/// How we read a site's robots.txt: which group is ours, which rule wins, what the wildcards mean. Written first:
/// <c>RobotsTxt.Parse</c> and <c>RobotsRules.Allows</c> threw NotImplementedException on the red run.
/// </summary>
public class RobotsTxtTests
{
    private const string Bobotie = "/2018/09/26/traditional-south-african-bobotie-with-fragrant-yellow-rice/";

    /// <summary>The tokens as the test cases below take them.</summary>
    private static IEnumerable<string> NamedAiReaders => RobotsTxt.AiReaders;

    /// <summary>One of the real files checked in step 0, by the site's name.</summary>
    private static string Fixture(string site) => File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "tests",
        "louis-agent.core.tests", "Tools", "Web", "Robots", "Fixtures", site + ".robots.txt"));

    /// <summary>The rules that bind us in a file, read the way the policy reads it.</summary>
    private static RobotsRules Ours(string text) => RobotsTxt.Parse(text, RobotsTxt.UserAgentToken);

    /// <summary>
    /// Drizzle and Dip's own file: the * group's rules apply to us, the MJ12bot group is somebody else's.
    /// </summary>
    [Test]
    public void DrizzleAndDipsFile_StarGroupBindsUs_AndTheLongerAllowWins()
    {
        var rules = Ours(Fixture("drizzleanddip"));

        Assert.That(rules.Allows(Bobotie), Is.True);
        Assert.That(rules.Allows("/wp-admin/"), Is.False);
        Assert.That(rules.Allows("/wp-admin/admin-ajax.php"), Is.True, "the longer Allow beats the shorter Disallow");
        Assert.That(rules.Allows("/?s=bobotie"), Is.True);
        Assert.That(rules.Allows("/"), Is.True, "MJ12bot's 'Disallow: /' is not ours");
    }

    /// <summary>The other two real files let us read a recipe page and a search, and keep us out of wp-admin.</summary>
    [TestCase("thelazymakoti")]
    [TestCase("recipesfromsouthafrica")]
    public void TheOtherRealFiles_LetUsReadPagesAndSearches(string site)
    {
        var rules = Ours(Fixture(site));

        Assert.That(rules.Allows("/recipe/chakalaka/"), Is.True);
        Assert.That(rules.Allows("/?s=melktert"), Is.True);
        Assert.That(rules.Allows("/wp-admin/"), Is.False);
    }

    /// <summary>
    /// If the file has a group for louis-agent we follow that one and ignore *, whatever the token's case.
    /// </summary>
    [Test]
    public void OurOwnGroup_IsTaken_AndTheStarGroupIgnored()
    {
        const string text = "User-agent: *\nDisallow: /\n\nUser-agent: Louis-Agent\nDisallow: /private/\n";

        var rules = Ours(text);

        Assert.That(rules.Allows("/recipes/"), Is.True);
        Assert.That(rules.Allows("/private/x"), Is.False);
    }

    /// <summary>A site that turns away the AI crawlers by name turns us away too, even if * lets everyone in.</summary>
    [Test]
    public void AGroupForAnAiReader_ThatDisallowsAll_DisallowsUsEverywhere()
    {
        const string text = "User-agent: *\nAllow: /\n\nUser-agent: GPTBot\nDisallow: /\n";

        var rules = Ours(text);

        Assert.That(rules.Allows("/"), Is.False);
        Assert.That(rules.Allows("/recipes/bobotie/"), Is.False);
    }

    /// <summary>With two AI groups the stricter reading holds: a path either of them closes is closed.</summary>
    [Test]
    public void TwoAiReaderGroups_TheStrictestApplies()
    {
        const string text = "User-agent: ClaudeBot\nDisallow: /recipes/\n\nUser-agent: CCBot\nDisallow: /?s=\n";

        var rules = Ours(text);

        Assert.That(rules.Allows("/recipes/bobotie/"), Is.False);
        Assert.That(rules.Allows("/?s=bobotie"), Is.False);
        Assert.That(rules.Allows("/about/"), Is.True);
    }

    /// <summary>
    /// One AI group allowing a path doesn't undo another group closing it; each group is read on its own.
    /// </summary>
    [Test]
    public void AnAiGroupThatAllows_DoesNotReopenWhatAnotherAiGroupCloses()
    {
        const string text = "User-agent: GPTBot\nDisallow: /\n\nUser-agent: ClaudeBot\nAllow: /recipes/\nDisallow: /\n";

        Assert.That(Ours(text).Allows("/recipes/bobotie/"), Is.False);
    }

    /// <summary>
    /// An AI group doesn't take the place of *: what the site closes to everyone stays closed to us too (N2).
    /// </summary>
    [Test]
    public void AnAiGroupBesideTheStarGroup_BothBindUs()
    {
        const string text = "User-agent: *\nDisallow: /wp-admin/\n\nUser-agent: GPTBot\nDisallow: /private/\n";

        var rules = Ours(text);

        Assert.That(rules.Allows("/wp-admin/"), Is.False);
        Assert.That(rules.Allows("/private/x"), Is.False);
        Assert.That(rules.Allows("/about/"), Is.True);
    }

    /// <summary>Our own group beside an AI group: we are named, so ours is the one that counts.</summary>
    [Test]
    public void OurGroupBesideAnAiGroup_OursWins()
    {
        const string text = "User-agent: GPTBot\nDisallow: /\n\nUser-agent: louis-agent\nDisallow: /private/\n";

        var rules = Ours(text);

        Assert.That(rules.Allows("/recipes/"), Is.True);
        Assert.That(rules.Allows("/private/"), Is.False);
    }

    /// <summary>Every AI reader the list names turns us away when its group is the only one that disallows.</summary>
    [TestCaseSource(nameof(NamedAiReaders))]
    public void EveryNamedAiReader_BindsUs(string token)
    {
        var rules = Ours($"User-agent: {token}\nDisallow: /\n");

        Assert.That(rules.Allows("/page/"), Is.False);
    }

    /// <summary>The ten tokens ADR 0068 names, so a change to the list is a deliberate one.</summary>
    [Test]
    public void TheAiReaderList_IsTheTenTheAdrNames()
    {
        Assert.That(RobotsTxt.AiReaders,
            Is.EquivalentTo(new[]
            {
                "GPTBot", "ClaudeBot", "anthropic-ai", "Claude-Web", "CCBot", "Google-Extended",
                "Applebot-Extended", "Bytespider", "PerplexityBot", "Meta-ExternalAgent",
            }));
    }

    /// <summary>
    /// * stands for any run of characters, so a rule can close a query string wherever the path goes.
    /// </summary>
    [Test]
    public void AStar_MatchesAnyRun()
    {
        var rules = Ours("User-agent: *\nDisallow: /*?add-to-cart=\n");

        Assert.That(rules.Allows("/shop/?add-to-cart=12"), Is.False);
        Assert.That(rules.Allows("/shop/"), Is.True);
    }

    /// <summary>
    /// $ pins a rule to the end of the address, so .css files are allowed and a .css with a query is not.
    /// </summary>
    [Test]
    public void ADollar_MeansTheEndOfTheAddress()
    {
        var rules = Ours("User-agent: *\nDisallow: /\nAllow: /*.css$\n");

        Assert.That(rules.Allows("/a/b.css"), Is.True);
        Assert.That(rules.Allows("/a/b.css?x"), Is.False);
    }

    /// <summary>Same length, Allow beats Disallow, as the RFC says.</summary>
    [Test]
    public void OnEqualLength_AllowWinsOverDisallow()
    {
        var rules = Ours("User-agent: *\nDisallow: /page\nAllow: /page\n");

        Assert.That(rules.Allows("/page"), Is.True);
    }

    /// <summary>"Disallow:" with nothing after it lets everything in.</summary>
    [Test]
    public void ADisallowWithNoPath_AllowsEverything()
    {
        Assert.That(Ours("User-agent: *\nDisallow:\n").Allows("/anything"), Is.True);
    }

    /// <summary>
    /// Comments, blank lines, odd spacing, field-name case, Crawl-delay and Sitemap lines don't matter.
    /// </summary>
    [Test]
    public void CommentsBlankLinesOddSpacing_AndUnknownFields_AreFine()
    {
        const string text = "﻿# our rules\n\nUSER-AGENT :   *   # everyone\r\n\r\n" +
                            "Crawl-delay: 10\n  disallow:/private/  # no\nSitemap: https://a.com/s.xml\nHost: a.com\n";

        var rules = Ours(text);

        Assert.That(rules.Allows("/private/x"), Is.False);
        Assert.That(rules.Allows("/public/"), Is.True);
    }

    /// <summary>Two User-agent lines in a row share the rules under them.</summary>
    [Test]
    public void SeveralUserAgentLines_ShareTheirRules()
    {
        var rules = Ours("User-agent: Googlebot\nUser-agent: louis-agent\nDisallow: /x/\n");

        Assert.That(rules.Allows("/x/y"), Is.False);
    }

    /// <summary>No group for us and no * group, or no text at all, means every path is allowed.</summary>
    [TestCase("User-agent: MJ12bot\nDisallow: /\n")]
    [TestCase("")]
    [TestCase("   \n\n")]
    public void NoGroupForUs_OrAnEmptyFile_AllowsAll(string text)
    {
        Assert.That(Ours(text).Allows("/any/path?q=1"), Is.True);
    }
}
