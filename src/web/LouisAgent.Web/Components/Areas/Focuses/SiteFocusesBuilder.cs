namespace LouisAgent.Web.Components.Areas.Focuses;

/// <summary>
/// Collects a site's focuses: the default its pages take, and each part's (F127.7). The site's areas hand it to
/// <see cref="SiteAreasBuilder.Focuses"/>.
/// </summary>
public sealed class SiteFocusesBuilder
{
    private readonly List<FocusPart> _parts = [];
    private string? _default;

    /// <summary>The focus a page takes when no part's address holds it.</summary>
    public SiteFocusesBuilder Default(string focus)
    {
        _default = focus;
        return this;
    }

    /// <summary>The focus every page at or under <paramref name="address"/> takes.</summary>
    public SiteFocusesBuilder Under(string address, string focus)
    {
        _parts.Add(new FocusPart(address, focus));
        return this;
    }

    /// <summary>The focuses collected.</summary>
    internal SiteFocuses Build() => new(_default, _parts.ToList());
}
