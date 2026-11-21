namespace LouisAgent.Site.Resumes.Skills;

/// <summary>
/// The CV's languages as one line of words (F142): the page names them in a line, and the editor retypes that line
/// with commas between them.
/// </summary>
public static class ResumeLanguages
{
    /// <summary>The languages as they are typed: their names, with commas.</summary>
    public static string Listed(IReadOnlyList<ResumeLanguage> languages) =>
        string.Join(", ", languages.Select(language => language.Language));

    /// <summary>
    /// The languages a typed line names, in its order. One the CV already holds is kept as it is, with what else
    /// it says (how well he speaks it), and a new name is a new language.
    /// </summary>
    public static IReadOnlyList<ResumeLanguage> Typed(string? typed, IReadOnlyList<ResumeLanguage> known) =>
    [
        .. (typed ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(name => known.FirstOrDefault(language =>
            string.Equals(language.Language, name, StringComparison.OrdinalIgnoreCase)) ?? new ResumeLanguage(name))
    ];
}
