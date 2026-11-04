namespace LouisAgent.Core.Tools.Web.Robots.Rules;

/// <summary>
/// Says whether a robots.txt pattern fits a path. A pattern fits when the path starts with it; <c>*</c> stands for
/// any run of characters and a final <c>$</c> means the path must end there.
/// </summary>
/// <remarks>
/// A plain walk over the pattern and the path, no regular expression: robots.txt has only two special characters, and
/// an outside site writes the pattern, so a pattern language that can backtrack has no business reading it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: regex is the last option</seealso>
internal static class RobotsPattern
{
    /// <summary>
    /// Whether the path fits the pattern: <c>/*.css$</c> fits <c>/a/b.css</c> and not <c>/a/b.css?x</c>.
    /// </summary>
    public static bool Matches(string pattern, string path)
    {
        bool anchored = pattern.EndsWith('$');
        string body = anchored ? pattern[..^1] : pattern;
        int at = 0, onPath = 0, star = -1, starPath = 0;

        while (onPath < path.Length)
        {
            // The whole pattern is used up and it doesn't have to reach the end: the path starts with it.
            if (at == body.Length && !anchored) return true;

            // A star: remember where it is, and first try it as matching nothing.
            if (at < body.Length && body[at] == '*')
            {
                star = at++;
                starPath = onPath;
                continue;
            }

            // A plain character that agrees with the path: both move on.
            if (at < body.Length && body[at] == path[onPath])
            {
                at++;
                onPath++;
                continue;
            }

            // A mismatch: the last star takes one more character of the path, or there is no way to fit.
            if (star < 0) return false;

            at = star + 1;
            onPath = ++starPath;
        }

        // The path is used up: the rest of the pattern may only be stars.
        while (at < body.Length && body[at] == '*') at++;
        return at == body.Length;
    }
}
