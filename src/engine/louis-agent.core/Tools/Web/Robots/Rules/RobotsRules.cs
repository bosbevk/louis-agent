namespace LouisAgent.Core.Tools.Web.Robots.Rules;

/// <summary>
/// The rules that bind us on one site, ready to ask about a path. The longest rule that fits the path decides; if an
/// Allow and a Disallow fit equally well, Allow wins; a path no rule fits is allowed.
/// </summary>
/// <remarks>
/// Value object with a small Composite. Usually it holds one group's rules. When AI-reader groups bind us beside
/// <c>*</c> they are not merged into one list (an Allow in one group would then undo a Disallow in another); each
/// stays its own <see cref="RobotsRules"/> in <paramref name="AlsoBinding"/>, and a path is allowed only if every one
/// allows it.
/// That is the strictest reading, on purpose (ADR 0068).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0068-every-fetch-obeys-robots-txt.md">ADR 0068: every fetch obeys robots.txt</seealso>
/// <seealso href="https://datatracker.ietf.org/doc/html/rfc9309#name-the-allow-and-disallow-lines">RFC 9309, the Allow and Disallow lines</seealso>
public sealed record RobotsRules(IReadOnlyList<RobotsRule> Rules, IReadOnlyList<RobotsRules>? AlsoBinding = null)
{
    /// <summary>No rules at all: every path is allowed.</summary>
    public static RobotsRules None { get; } = new([]);

    /// <summary>
    /// Whether we may read this path and query (<c>/page?x=1</c>), by these rules and by every group that also
    /// binds us.
    /// </summary>
    public bool Allows(string pathAndQuery) =>
        AllowedByOwnRules(pathAndQuery) && (AlsoBinding ?? []).All(group => group.Allows(pathAndQuery));

    /// <summary>
    /// Walks every rule and keeps the longest pattern that fits; a tie goes to Allow, and none fitting is allowed.
    /// </summary>
    private bool AllowedByOwnRules(string pathAndQuery)
    {
        int longest = -1;
        bool allowed = true;
        foreach (var rule in Rules)
        {
            if (!RobotsPattern.Matches(rule.Pattern, pathAndQuery)) continue;

            bool longer = rule.Pattern.Length > longest;
            bool tieForAllow = rule.Pattern.Length == longest && rule.Allow;
            if (!longer && !tieForAllow) continue;

            longest = rule.Pattern.Length;
            allowed = rule.Allow;
        }

        return allowed;
    }
}
