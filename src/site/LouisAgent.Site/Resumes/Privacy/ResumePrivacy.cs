namespace LouisAgent.Site.Resumes.Privacy;

/// <summary>
/// Finds what a public CV must not hold (F142): an ID number, a salary, a birth date or a street address. The
/// embedded file's test and a save from the admin's editor both ask it, so neither lets one through.
/// </summary>
/// <remarks>
/// It reads the file's words, not its records: the records have no field for any of these, so the strict read
/// already refuses a new field, and this catches one typed into a summary or a highlight.
/// </remarks>
public static class ResumePrivacy
{
    /// <summary>How many digits a South African ID number has.</summary>
    private const int IdNumberDigits = 13;

    /// <summary>The words that give each problem away, in lower case.</summary>
    private static readonly (string Word, ResumePrivacyProblem Problem)[] Words =
    [
        ("id number", ResumePrivacyProblem.IdNumber),
        ("salary", ResumePrivacyProblem.Salary),
        ("per month", ResumePrivacyProblem.Salary),
        ("birth", ResumePrivacyProblem.BirthDate),
        ("\"address\"", ResumePrivacyProblem.Address),
        ("street", ResumePrivacyProblem.Address)
    ];

    /// <summary>What the file holds that a public page mustn't show, each problem once; none when it's clean.</summary>
    public static IReadOnlyList<ResumePrivacyProblem> Problems(string json)
    {
        string text = json.ToLowerInvariant();
        var found = Words.Where(entry => text.Contains(entry.Word)).Select(entry => entry.Problem).ToList();
        if (LongestDigitRun(text) >= IdNumberDigits) found.Add(ResumePrivacyProblem.IdNumber);

        return [.. found.Distinct()];
    }

    /// <summary>The longest run of digits in the text, the spaces inside a run not ending it.</summary>
    private static int LongestDigitRun(string text)
    {
        int longest = 0, run = 0;
        foreach (char c in text)
        {
            // A digit grows the run, a space keeps it (an ID number written in groups), anything else ends it.
            if (char.IsDigit(c)) run++;
            else if (c != ' ') run = 0;
            longest = Math.Max(longest, run);
        }

        return longest;
    }
}
