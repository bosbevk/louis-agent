namespace LouisAgent.Core.Tools.Web.Robots;

/// <summary>
/// What a site's <c>robots.txt</c> says about one address: read it, leave it alone, or we couldn't find out because
/// the file itself couldn't be read.
/// </summary>
public enum RobotsVerdict
{
    /// <summary>The file lets us read this path, or there is no file to say otherwise.</summary>
    Allowed,

    /// <summary>The file asks us to leave this path alone.</summary>
    Disallowed,

    /// <summary>
    /// The file couldn't be read (a server error or no answer), so nothing of the site is read for now.
    /// </summary>
    Unreachable,
}
