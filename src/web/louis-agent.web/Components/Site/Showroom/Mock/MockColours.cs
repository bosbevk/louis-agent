namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// A mock website's own colours (F127.1): the business's brand (its header, its customer's messages), the text on it,
/// the page and the page's text. They are the business's look, not the site's theme, so they are the page's data, each
/// written as six hex digits; anything else is refused, since they are set into the mock's style.
/// </summary>
public sealed record MockColours
{
    /// <summary>The colours, each checked.</summary>
    public MockColours(string brand, string onBrand, string page, string ink)
    {
        Brand = Checked(brand, nameof(brand));
        OnBrand = Checked(onBrand, nameof(onBrand));
        Page = Checked(page, nameof(page));
        Ink = Checked(ink, nameof(ink));
    }

    /// <summary>The business's brand colour: its header and its customer's messages.</summary>
    public string Brand { get; }

    /// <summary>The text on the brand colour.</summary>
    public string OnBrand { get; }

    /// <summary>The page behind the hero.</summary>
    public string Page { get; }

    /// <summary>The page's text.</summary>
    public string Ink { get; }

    /// <summary>A colour as six hex digits after a <c>#</c>, or a refusal naming the parameter.</summary>
    private static string Checked(string colour, string name) =>
        colour.Length == 7 && colour[0] == '#' && colour.Skip(1).All(char.IsAsciiHexDigit)
            ? colour
            : throw new ArgumentException(null, name);
}
