namespace LouisAgent.Web.Git;

/// <summary>A commit's date as the panels say it (W2b, from BranchesPanel).</summary>
public static class RelativeTime
{
    /// <summary>"just now", then minutes, hours and days ago up to a month, then the date; what isn't a date as it is.</summary>
    public static string Ago(string isoDate) => Ago(isoDate, DateTimeOffset.Now);

    public static string Ago(string isoDate, DateTimeOffset now)
    {
        if (!DateTimeOffset.TryParse(isoDate, out var date)) return isoDate;
        var age = now - date;
        return age.TotalMinutes < 1 ? "just now"
            : age.TotalHours < 1 ? $"{(int)age.TotalMinutes}m ago"
            : age.TotalDays < 1 ? $"{(int)age.TotalHours}h ago"
            : age.TotalDays < 30 ? $"{(int)age.TotalDays}d ago"
            : date.ToString("yyyy-MM-dd");
    }
}
