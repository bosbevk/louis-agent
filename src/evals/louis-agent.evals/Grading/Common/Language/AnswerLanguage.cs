using LouisAgent.Choices;

namespace LouisAgent.Evals.Grading.Common.Language;

/// <summary>
/// A language an answer can be asked to be in, by its ISO 639-1 code: the ones the harness has a word list for.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum AnswerLanguage
{
    [WireName("af")] Afrikaans,
    [WireName("en")] English,
}
