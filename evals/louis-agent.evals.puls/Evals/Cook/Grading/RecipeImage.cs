using System.Collections.Frozen;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Shared.Scoring;
using LouisAgent.Evals.Puls.Shared.Turns;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading;

/// <summary>
/// The first turn's answer shows at least one image of the recipe (F18 step 5.7; Louis, 2026-10-25: "i want at least one image to render from the recipe"),
/// and the image is one the recipe page gave: <c>![alt](url)</c> in the text FetchUrl or ReadRenderedPage returned, never a link the model made up. Required where
/// a scenario sets <c>recipe_image</c>. When none of the pages the cook read had an image there is nothing to show, so the grade is Unknown, never a Fail.
/// A fixed rule over the answer and the results, so it needs no model.
/// </summary>
public sealed class RecipeImage : ICheck
{
    public string Name => "recipe-image";
    public int Version => 2;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => true;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "recipe_image" }.ToFrozenSet();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    private Grade Grade(Trial trial)
    {
        var turns = trial.Transcript.Turns;
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;
        if (!Asked(trial.Scenario.Expected))
            return Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.RecipeImageNotSet));

        var view = turns.Select(TurnView.From).FirstOrDefault();
        if (view is null || view.Error is not null || view.Answer.Length == 0)
            return Verdict.Unknown(Name, Version, MessageText.Format(GradeMessage.ShopNothingToRead, new { Turn = 1 }));

        var onPages = CitedPages(view).SelectMany(Links.RecipePhotos).ToHashSet();
        if (onPages.Count == 0)
            return Verdict.Unknown(Name, Version, MessageText.Format(GradeMessage.RecipeImageNoneOnPages));

        var shown = Links.RecipePhotos(view.Answer);
        if (shown.Count == 0) return Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.RecipeImageMissing));
        return shown.Any(onPages.Contains)
            ? Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.RecipeImageRight))
            : Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.RecipeImageNotFromPage));
    }

    /// <summary>
    /// The pages whose images count (version 2): the pages the answer links to, found by the <c>URL:</c> line a fetched page's text starts with. An answer built on a
    /// page with no photo is not failed for the photo of another page the cook only read on the way. When the answer links to no page the cook fetched (or the
    /// results name none), every result counts, as in version 1.
    /// </summary>
    private static IReadOnlyList<string> CitedPages(TurnView view)
    {
        var results = view.Results.Concat(view.SpecialistResults).ToList();
        string answer = Plain(view.Answer);
        var cited = results.Where(result =>
            UrlOf(result) is { } url && answer.Contains(Plain(url), StringComparison.OrdinalIgnoreCase)).ToList();
        return cited.Count > 0 ? cited : results;
    }

    private static string? UrlOf(string result)
    {
        const string Marker = "URL:";
        if (!result.StartsWith(Marker, StringComparison.Ordinal)) return null;
        string line = result.Split('\n', 2)[0][Marker.Length..].Trim();
        return line.Length > 0 ? line : null;
    }

    /// <summary>An address without its scheme, its <c>www.</c> and its closing slash, so the same page written two ways is the same page.</summary>
    private static string Plain(string text) =>
        text.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');

    private static bool Asked(JsonElement expected) =>
        expected.ValueKind == JsonValueKind.Object && expected.TryGetProperty("recipe_image", out var value) &&
        value.ValueKind == JsonValueKind.True;
}
