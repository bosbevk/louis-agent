using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Language;

/// <summary>
/// The first turn's answer is in the language asked for: it has more of that language's common small words than of
/// any other's. An answer with too few of them to tell, or as many of two languages', is Unknown, never a guess. A
/// turn the infrastructure broke is Unknown; a turn that streamed an error fails.
/// </summary>
/// <remarks>
/// A Specification over the answer (Evans and Fowler) with a word-list count, Louis's pick over asking the judge: a
/// model call to tell Afrikaans from English is waste, and a count is free and instant. The lists are in
/// <see cref="LanguageWords"/>. Its <see cref="Entry"/> builds it from a file's settings (Factory Method), the
/// language by its code.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class AnswersInLanguageCheck(AnswerLanguage language, bool required = true) : ICheck
{
    /// <summary>The fewest common words an answer needs before its language is told: below it, a guess.</summary>
    private const int FewestWords = 5;

    /// <inheritdoc />
    public string Name => "answers-in-language";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Counts the first turn's common words, after making sure that turn can be judged.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(GradeFirst(trial.Transcript.Turns.FirstOrDefault()));

    /// <summary>
    /// The grade of the first turn: Unknown with no turn or a broken one, a fail on a streamed error, then by the
    /// count: Unknown with too few words or a tie, a fail when another language has more, else a pass.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade GradeFirst(TurnTranscript? first)
    {
        if (first is null) return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoTurn));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return unknown;
        if (EngineEvents.StreamedError(first))
            return Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TurnFailed));

        var words = Words(EngineEvents.AnswerOf(first));
        var count = Count(words, language);
        var (other, otherCount) = Enum.GetValues<AnswerLanguage>().Where(each => each != language)
            .Select(each => (Language: each, Count: Count(words, each))).MaxBy(each => each.Count);
        var holes = new
        {
            Language = LanguageWords.NameOf(language),
            Count = count,
            Other = LanguageWords.NameOf(other),
            OtherCount = otherCount
        };

        if (count + otherCount < FewestWords)
            return Verdict.Unknown(Name, Version,
                MessageText.Format(CommonGradeMessage.LanguageTooFewWords, new { Count = count + otherCount }));
        if (count == otherCount)
            return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.LanguageTied, holes));
        return count > otherCount
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.InLanguagePass, holes))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.InOtherLanguage, holes));
    }

    /// <summary>
    /// The answer's words in lower case: the runs of letters between everything else, so <c>'n</c> is <c>n</c> and
    /// <c>hy's</c> is <c>hy</c> and <c>s</c>.
    /// </summary>
    private static IReadOnlyList<string> Words(string answer)
    {
        var words = new List<string>();
        var start = -1;
        // One past the end, so a word the answer ends on is read too.
        for (var i = 0; i <= answer.Length; i++)
        {
            var letter = i < answer.Length && char.IsLetter(answer[i]);
            if (letter && start < 0) start = i;
            if (letter || start < 0) continue;

            words.Add(answer[start..i].ToLowerInvariant());
            start = -1;
        }

        return words;
    }

    /// <summary>How many of the words are on the language's list, each time it's said.</summary>
    private static int Count(IReadOnlyList<string> words, AnswerLanguage of) =>
        words.Count(LanguageWords.For(of).Contains);

    /// <summary>
    /// What a file can set: the language's code (<c>af</c>, <c>en</c>) and whether the check must pass.
    /// </summary>
    public sealed record Settings(string Language, bool Required = true);

    /// <summary>The check as a file names it, <c>answers-in-language</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("answers-in-language", CheckKind.FixedRule)
    {
        /// <summary>
        /// Builds the check from its settings, refusing a language the harness has no word list for, naming the ones
        /// it has.
        /// </summary>
        protected override ICheck Create(Settings settings, CheckContext context) =>
            SettingChoice.TryParse(settings.Language, out AnswerLanguage? parsed) && parsed is { } chosen
                ? new AnswersInLanguageCheck(chosen, settings.Required)
                : throw new InvalidDataException(MessageText.Format(CheckCatalogMessage.NotAChoice, new
                {
                    Type,
                    Value = settings.Language,
                    Field = "language",
                    Supported = string.Join(", ", SettingChoice.Names<AnswerLanguage>())
                }));
    }
}
