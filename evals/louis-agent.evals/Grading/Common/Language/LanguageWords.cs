using System.Collections.Frozen;

namespace LouisAgent.Evals.Grading.Common.Language;

/// <summary>
/// Each language's most common small words (articles, pronouns, little verbs and joining words), which any answer in
/// that language is full of, whatever it's about. No word is on two lists: a word both languages use (<c>is</c>,
/// <c>in</c>, <c>was</c>, <c>my</c>) says nothing about which one it is, so it's on neither.
/// </summary>
/// <remarks>
/// A language is added with one list here and one member of <see cref="AnswerLanguage"/>. The lists are small on
/// purpose: a long one starts catching content words, which travel between languages (a dish's name, a shop's).
/// </remarks>
public static class LanguageWords
{
    /// <summary>The lists, by language.</summary>
    private static readonly IReadOnlyDictionary<AnswerLanguage, FrozenSet<string>> Lists =
        new Dictionary<AnswerLanguage, FrozenSet<string>>
        {
            [AnswerLanguage.Afrikaans] =
                new[]
                {
                    "die", "en", "van", "nie", "het", "dit", "wat", "met", "vir", "op", "te", "sal", "kan",
                    "ons", "jy", "jou", "ek", "hulle", "maar", "ook", "nog", "baie", "aan", "om", "word",
                    "hierdie", "daar", "net", "na", "tot", "sy", "hy", "moet", "gaan", "n", "een", "wees",
                    "hier", "nou", "se"
                }.ToFrozenSet(),
            [AnswerLanguage.English] = new[]
            {
                "the", "and", "of", "to", "a", "that", "it", "for", "you", "with", "on", "are", "this", "be",
                "at", "have", "not", "but", "your", "from", "they", "or", "will", "can", "we", "if", "an",
                "about", "there", "what", "which", "would", "should", "has", "these", "its", "our", "their",
                "them", "all"
            }.ToFrozenSet()
        };

    /// <summary>The language's list of small words, in lower case.</summary>
    public static IReadOnlySet<string> For(AnswerLanguage language) => Lists[language];

    /// <summary>The language's name, in English, for a grade's reason.</summary>
    public static string NameOf(AnswerLanguage language) => language switch
    {
        AnswerLanguage.Afrikaans => "Afrikaans",
        _ => "English"
    };
}
