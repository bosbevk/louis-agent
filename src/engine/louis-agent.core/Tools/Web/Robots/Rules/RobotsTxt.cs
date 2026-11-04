namespace LouisAgent.Core.Tools.Web.Robots.Rules;

/// <summary>
/// Reads a site's robots.txt and works out which of its rules bind us. Our own group (<c>louis-agent</c>) if the file
/// has one; otherwise the <c>*</c> group and every group of an AI reader it names, all together, so a path any of
/// them closes is closed to us; with none of those, every path is allowed.
/// </summary>
/// <remarks>
/// Parser: the file is cut into groups by its <c>User-agent</c> lines and each group keeps its Allow and Disallow
/// lines; everything else (<c>Crawl-delay</c>, <c>Sitemap</c>, a field nobody knows) is skipped. Reading the AI-reader
/// groups beside <c>*</c> goes beyond RFC 9309 on purpose: we are an AI reader, so a site that says no to the named
/// ones says no to us, and without this a file that only blocks GPTBot would let us in; <c>*</c> still binds, so
/// what the site closes to everyone stays closed (Louis's pick N2). The list is code, not a
/// setting, so it changes in a commit someone reviews (ADR 0068).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0068-every-fetch-obeys-robots-txt.md">ADR 0068: every fetch obeys robots.txt</seealso>
/// <seealso href="https://datatracker.ietf.org/doc/html/rfc9309#name-the-user-agent-line">RFC 9309, the User-agent line</seealso>
public static class RobotsTxt
{
    /// <summary>The name a site's robots.txt can give us: the product token of our user agent.</summary>
    public const string UserAgentToken = "louis-agent";

    /// <summary>
    /// The AI readers a site may name; a group for one of them binds us when we have no group of our own.
    /// </summary>
    public static IReadOnlyList<string> AiReaders { get; } =
    [
        "GPTBot", "ClaudeBot", "anthropic-ai", "Claude-Web", "CCBot", "Google-Extended", "Applebot-Extended",
        "Bytespider", "PerplexityBot", "Meta-ExternalAgent",
    ];

    /// <summary>
    /// The rules of the file that bind a reader with this token, by the order in the summary above.
    /// </summary>
    public static RobotsRules Parse(string text, string userAgentToken)
    {
        var groups = Groups(text);

        var ours = groups.Where(g => g.Names(userAgentToken)).ToList();
        if (ours.Count > 0) return Merged(ours);

        // The * group's rules, and each AI reader's group on its own beside them: a path any of them closes is closed.
        var everyone = Merged(groups.Where(g => g.Names("*")).ToList());
        var aiReaders = groups.Where(g => AiReaders.Any(g.Names)).Select(g => new RobotsRules(g.Rules)).ToList();
        return aiReaders.Count > 0 ? everyone with { AlsoBinding = aiReaders } : everyone;
    }

    /// <summary>
    /// Several groups for the same reader are one group (RFC 9309): their rules are read together.
    /// </summary>
    private static RobotsRules Merged(List<Group> groups) => new(groups.SelectMany(g => g.Rules).ToList());

    /// <summary>
    /// Cuts the file into groups: a run of User-agent lines starts one, the Allow and Disallow lines after it fill it.
    /// </summary>
    private static List<Group> Groups(string text)
    {
        var groups = new List<Group>();
        Group? current = null;
        bool lastWasAgent = false;

        foreach (string raw in text.TrimStart('\uFEFF').Split('\n'))
        {
            if (Field(raw) is not (var field, var value)) continue;

            switch (field)
            {
                // A User-agent line right after another joins its group; after rules it starts a new one.
                case "user-agent" when lastWasAgent && current is not null:
                    current.Agents.Add(value);
                    break;
                case "user-agent":
                    current = new Group();
                    current.Agents.Add(value);
                    groups.Add(current);
                    lastWasAgent = true;
                    break;
                case "allow" or "disallow":
                    lastWasAgent = false;
                    // An empty path ("Disallow:") says nothing, so it adds no rule.
                    if (current is not null && value.Length > 0)
                        current.Rules.Add(new RobotsRule(field == "allow", value));
                    break;
            }
        }

        return groups;
    }

    /// <summary>
    /// The field name (lower case) and value of a line, without its comment; none for a line that isn't one.
    /// </summary>
    private static (string Field, string Value)? Field(string line)
    {
        int comment = line.IndexOf('#');
        string content = comment < 0 ? line : line[..comment];
        int colon = content.IndexOf(':');
        if (colon < 0) return null;

        return (content[..colon].Trim().ToLowerInvariant(), content[(colon + 1)..].Trim());
    }

    /// <summary>
    /// One group of the file: the names it is addressed to and the rules under them.
    /// </summary>
    private sealed class Group
    {
        /// <summary>The User-agent names the group is written for.</summary>
        public List<string> Agents { get; } = [];

        /// <summary>The group's Allow and Disallow lines, in the file's order.</summary>
        public List<RobotsRule> Rules { get; } = [];

        /// <summary>Whether the group is addressed to this token, whatever the case.</summary>
        public bool Names(string token) => Agents.Contains(token, StringComparer.OrdinalIgnoreCase);
    }
}
