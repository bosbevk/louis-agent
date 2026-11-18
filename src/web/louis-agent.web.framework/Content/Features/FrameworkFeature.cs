using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content.Features;

/// <summary>
/// One thing the framework does (F161.1, step 11): the area it is in, its name, the setting, type or call that does
/// it, a line on what it does, and the doc that tells it, by its path in the repo.
/// </summary>
/// <remarks>
/// The code is the feature's proof: the page's test finds it in the framework's source and the doc on the disk, so a
/// feature that is taken out fails the page instead of staying on it.
/// </remarks>
public sealed record FrameworkFeature(FeatureArea Area, string Name, string Code, string What, string Doc)
{
    /// <summary>The feature as a term list shows it: its code, then its name in bold and what it does.</summary>
    public Term Term => new([Code], $"**{Name}.** {What}");
}
