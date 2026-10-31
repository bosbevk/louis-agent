using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Compare;

/// <summary>
/// The first answer shows an image, and it's one a page the agent read gave, never an address the model made up. The
/// pages that count are the ones the answer links to; an answer that links to none of them is checked against them
/// all. When no page had an image there was nothing to show, so the grade is Unknown, never a fail. With a key it
/// asks only the scenarios that set that key to true, and passes the rest.
/// </summary>
/// <remarks>
/// Extract, then compare, as <see cref="AmountUnderCheck"/> is: an <see cref="IImageReader"/> (a Strategy, named in
/// the file) says what counts as an image, on the pages and in the answer alike, so Puls's reader can leave out a
/// recipe page's share buttons and logos. It grades as the cook's own <c>recipe-image</c> did at its version 2, step
/// for step, so the cook names it under that name, over Puls's reader, with its grades unchanged. The key is
/// optional (Louis's pick): left out, every scenario is asked. Its <see cref="Entry"/> builds it from a file's
/// settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class ImageShownCheck(IImageReader reader, string? key = null, bool required = true) : ICheck
{
    /// <summary>The line a fetched page's text starts with, naming its address.</summary>
    private const string UrlLine = "URL:";

    /// <inheritdoc />
    public string Name => "image-shown";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } =
        key is null ? new HashSet<string>() : new HashSet<string> { key };

    /// <summary>Looks for an image from the pages the agent read in the first turn's answer.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    /// <summary>
    /// Unknown for a broken turn, nothing to read or no image on the pages; a pass when not asked or a page's image
    /// is shown; else a fail saying whether none was shown or one not from a page.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade Grade(Trial trial)
    {
        var turns = trial.Transcript.Turns;
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;
        if (!Asked(trial.Scenario.Expected))
            return Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.ImageNotAsked, new { Key = key }));

        var first = turns.FirstOrDefault();
        if (first is null || EngineEvents.StreamedError(first) || EngineEvents.AnswerOf(first).Length == 0)
            return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.ImageNothingToRead));

        var answer = EngineEvents.AnswerOf(first);
        var onPages = CitedPages(first, answer).SelectMany(reader.Images).ToHashSet();
        if (onPages.Count == 0)
            return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.ImageNoneOnPages));

        var shown = reader.Images(answer);
        if (shown.Count == 0)
            return Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.ImageMissing));
        return shown.Any(onPages.Contains)
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.ImageShownPass))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.ImageNotFromPage));
    }

    /// <summary>
    /// The pages whose images count: the ones the answer links to, found by the <c>URL:</c> line a fetched page's
    /// text starts with, so an answer built on a page with no image isn't failed for the image of another page the
    /// agent only read on the way. When the answer links to none of them, every result counts.
    /// </summary>
    private static IReadOnlyList<string> CitedPages(TurnTranscript turn, string answer)
    {
        var results = EngineEvents.ToolResultsOf(turn);
        var plain = Plain(answer);
        var cited = results.Where(result =>
            UrlOf(result) is { } url && plain.Contains(Plain(url), StringComparison.OrdinalIgnoreCase)).ToList();
        return cited.Count > 0 ? cited : results;
    }

    /// <summary>The address on a fetched page's first line, or null when it has none.</summary>
    private static string? UrlOf(string result)
    {
        if (!result.StartsWith(UrlLine, StringComparison.Ordinal)) return null;
        var url = result.Split('\n', 2)[0][UrlLine.Length..].Trim();
        return url.Length > 0 ? url : null;
    }

    /// <summary>
    /// An address without its scheme, its <c>www.</c> and its closing slash, so the same page written two ways is
    /// the same page.
    /// </summary>
    private static string Plain(string text) =>
        text.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');

    /// <summary>Whether the scenario is asked: always without a key, else when it sets the key to true.</summary>
    private bool Asked(JsonElement expected) =>
        key is null || (expected.ValueKind == JsonValueKind.Object && expected.TryGetProperty(key, out var value) &&
                        value.ValueKind == JsonValueKind.True);

    /// <summary>
    /// What a file can set: the reader that finds the images, the scenario key that asks for one (none: every
    /// scenario), and whether the check must pass.
    /// </summary>
    public sealed record Settings(string Reader, string? Key = null, bool Required = true);

    /// <summary>The check as a file names it, <c>image-shown</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("image-shown", CheckKind.FixedRule)
    {
        /// <summary>
        /// Builds the check from its settings, through the image reader the file names; a blank reader, or a key
        /// that is set but blank, is refused, as is a reader the catalogue doesn't have or one that reads no images.
        /// </summary>
        /// <remarks>Guard clauses, one blank setting each from the top (ADR 0051).</remarks>
        protected override ICheck Create(Settings settings, CheckContext context)
        {
            if (string.IsNullOrWhiteSpace(settings.Reader)) throw Refused(CheckCatalogMessage.NoText, "reader");
            if (settings.Key is not null && string.IsNullOrWhiteSpace(settings.Key)) throw Refused(CheckCatalogMessage.NoText, "key");
            return new ImageShownCheck(context.Readers.Get<IImageReader>(settings.Reader), settings.Key,
                settings.Required);
        }
    }
}
