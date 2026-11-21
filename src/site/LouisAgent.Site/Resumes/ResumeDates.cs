using System.Globalization;

namespace LouisAgent.Site.Resumes;

/// <summary>
/// The CV's dates for a reader (F142): JSON Resume writes a year and month as 2021-03, the page shows Mar 2021; a year
/// alone stays a year, and a job that hasn't ended runs to the word the page gives for now.
/// </summary>
/// <remarks>
/// A plain class, so the dates are tested without a component. They are calendar months, not moments, so they are
/// shown as written, never converted between time zones (ADR 0046 is about instants).
/// </remarks>
public static class ResumeDates
{
    /// <summary>A year and month as its month's short name and the year; anything else as written.</summary>
    public static string Of(string date) =>
        DateOnly.TryParseExact(date, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month)
            ? month.ToString("MMM yyyy", CultureInfo.InvariantCulture)
            : date;

    /// <summary>
    /// From the start to the end; to <paramref name="now"/> when there's no end; one date when they're the same, or
    /// when there's only an end.
    /// </summary>
    /// <remarks>Guard clauses, one per shape of span (ADR 0051).</remarks>
    public static string Span(string? start, string? end, string now)
    {
        if (start is null) return end is null ? "" : Of(end);
        if (end is null) return $"{Of(start)} – {now}";
        if (start == end) return Of(start);

        return $"{Of(start)} – {Of(end)}";
    }
}
