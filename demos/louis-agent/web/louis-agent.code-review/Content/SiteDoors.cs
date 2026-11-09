namespace LouisAgent.CodeReview.Content;

/// <summary>
/// What the site opens to a visitor today (FX88). The coding agent's live chat stays closed to strangers: its links
/// lead to the recorded session instead (F120, ADR 0108), and the chat's page sends a visitor back to the landing.
/// </summary>
/// <remarks>
/// One place that says what is open, read by the link and by the page, so the two can't disagree. A flag in code and
/// not a setting, because opening the chat is a release decision, made in a commit, not something an instance
/// configures (Louis, 2026-11-08: "have this as coming soon and make it not accessible until the feature is done").
/// </remarks>
public static class SiteDoors
{
    /// <summary>Whether a visitor may open the coding agent's chat.</summary>
    public static bool CodingAgent => false;
}
