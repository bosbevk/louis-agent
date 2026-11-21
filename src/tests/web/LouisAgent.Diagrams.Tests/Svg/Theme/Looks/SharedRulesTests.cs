using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

namespace LouisAgent.Diagrams.Tests.Svg.Theme.Looks;

/// <summary>
/// CL65.4: what two kinds of picture draw the same way is styled once, in a shared look. A flow and a class chart
/// each had the rule for the pill a word stands in, and three keys each the rule for the faint line over them, so
/// they looked alike only while the copies agreed.
/// </summary>
public class SharedRulesTests
{
    /// <summary>Each kind's own style, by the kind's name.</summary>
    private static readonly IReadOnlyDictionary<string, string> Kinds = new Dictionary<string, string>
    {
        ["flow"] = FlowStyle.Rules,
        ["classes"] = ClassesStyle.Of(SvgTheme.Site),
        ["sequence"] = SequenceStyle.Of(SvgTheme.Site),
        ["cards"] = CardsStyle.Rules,
        ["nested"] = NestedStyle.Rules,
        ["composite"] = CompositeStyle.Rules
    };

    /// <summary>The two looks every kind wears.</summary>
    private static readonly string Shared = PictureLook.Of(SvgTheme.Site) + MarqueeLook.Rules;

    /// <summary>
    /// A style's rules, each as what it sets, its spaces evened out: the part between a rule's braces. A rule
    /// that sets one thing only is left out, since two kinds may each fill a shape with nothing. So is a stop of
    /// a fade: a fade is a kind's own, and two fades may each end in the same colour gone to nothing.
    /// </summary>
    private static List<string> Bodies(string style) =>
    [
        .. style.Split('}').Where(rule => rule.Contains('{') && !rule.Contains('@'))
            .Select(rule => rule[(rule.LastIndexOf('{') + 1)..])
            .Select(body => string.Join(' ', body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))
            .Where(body => body.Count(letter => letter == ';') > 1)
            .Where(body => !body.StartsWith("stop-color", StringComparison.Ordinal))
    ];

    /// <summary>No kind's style sets what a rule of a shared look already sets.</summary>
    [Test]
    public void NoKindsStyle_HasARuleASharedLookHas()
    {
        var shared = Bodies(Shared).ToHashSet(StringComparer.Ordinal);

        var twice = Kinds.SelectMany(kind => Bodies(kind.Value).Where(shared.Contains)
            .Select(body => $"{kind.Key}: {body}"));

        Assert.That(twice, Is.Empty, "a rule a kind has and a shared look has: the kind wears the shared one");
    }

    /// <summary>No two kinds' styles have the same rule: what two kinds share is a shared look's.</summary>
    [Test]
    public void NoTwoKindsStyles_HaveTheSameRule()
    {
        var twice = Kinds.SelectMany(kind => Bodies(kind.Value).Distinct().Select(body => (kind.Key, Body: body)))
            .GroupBy(rule => rule.Body, StringComparer.Ordinal).Where(same => same.Count() > 1)
            .Select(same => $"{string.Join(" and ", same.Select(rule => rule.Key))}: {same.Key}");

        Assert.That(twice, Is.Empty, "a rule two kinds each have: it belongs in a shared look");
    }
}
