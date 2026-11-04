using LouisAgent.Composition.Messages;
using System.Collections.Frozen;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Checks;

/// <summary>
/// The cook's recipe comes from a recipe site we may read: the link in the first turn's answer is on a site of the
/// sites table. The grade says which site, by its short name, or which host the link was on instead. No link is a
/// fail, and a first turn that broke is Unknown, never a fail. Required.
/// </summary>
/// <remarks>
/// A fixed rule over the answer, the recipes pack's own (F35.2), listed in <see cref="RecipesChecks.All"/>. Which
/// hosts count is the pack's own knowledge: the sites table, read through <see cref="RecipeSites"/> as the factory
/// reads it, and each row's <see cref="RecipeSite.Covers"/>, the same rule the web search filters its results with.
/// How an answer writes its recipe link is the identity's, so an <see cref="ILinkReader"/> the eval file names finds
/// it (a Strategy, ADR 0059), as <c>estimates-at-shop</c> takes its price reader. Its <see cref="Entry"/> builds it
/// from a file's settings (Factory Method). The turn is read from the engine's events (<see cref="EngineEvents"/>).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0059-an-identity-ships-readers-not-checks.md">ADR 0059: an identity ships readers, not checks</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0060-a-pack-ships-its-checks.md">ADR 0060: a pack ships its checks</seealso>
public sealed class RecipeFromSource(ILinkReader links) : ICheck
{
    /// <inheritdoc />
    public string Name => "recipe-from-source";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => true;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys => FrozenSet<string>.Empty;

    /// <summary>Reads the first turn's answer, and only that: the recipe is the first answer's.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    /// <summary>
    /// Unknown for a first turn that broke or gave no answer; a fail for no link; then a pass naming the site the
    /// link is on, or a fail naming its host.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade Grade(Trial trial)
    {
        var turns = trial.Transcript.Turns;
        if (Verdict.InfrastructureFailure(Name, Version, turns.Take(1)) is { } unknown) return unknown;
        if (turns.Count == 0 || EngineEvents.StreamedError(turns[0]) || EngineEvents.AnswerOf(turns[0]).Length == 0)
            return Verdict.Unknown(Name, Version,
                MessageText.Format(RecipeCheckMessage.SourceNothingToRead, new { Turn = 1 }));

        if (links.RecipeLink(EngineEvents.AnswerOf(turns[0])) is not { } link)
            return Verdict.Fail(Name, Version, MessageText.Format(RecipeCheckMessage.SourceNoLink));

        Uri.TryCreate(link, UriKind.Absolute, out Uri? address);
        if (address is not null && SiteOf(address) is { } site)
            return Verdict.Pass(Name, Version,
                MessageText.Format(RecipeCheckMessage.SourceOnASite, new { Site = site.Slug }));

        return Verdict.Fail(Name, Version,
            MessageText.Format(RecipeCheckMessage.SourceOnAnotherHost,
                new { Host = address is null ? link : RecipeSite.HostOf(address) }));
    }

    /// <summary>The site of the table the address is on, or null when it is on none.</summary>
    private static RecipeSite? SiteOf(Uri address) => RecipeSites.All.FirstOrDefault(site => site.Covers(address));

    /// <summary>What a file can set: the reader that finds the recipe's link in an answer.</summary>
    public sealed record Settings(string Reader);

    /// <summary>The check as a file names it, <c>recipe-from-source</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("recipe-from-source", CheckKind.FixedRule)
    {
        /// <summary>
        /// Builds the check through the link reader the file names; a blank reader is refused, as is one the
        /// catalogue doesn't have or one that reads no links.
        /// </summary>
        protected override ICheck Create(Settings settings, CheckContext context) =>
            !string.IsNullOrWhiteSpace(settings.Reader)
                ? new RecipeFromSource(context.Readers.Get<ILinkReader>(settings.Reader))
                : throw Refused(CheckCatalogMessage.NoText, "reader");
    }
}
