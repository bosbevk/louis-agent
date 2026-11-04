using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Tests.Sources;

/// <summary>
/// Checks that the table of recipe sites is read once into rows, that a row says what the plan's table says, and that
/// a broken row is a loud error and never a silent skip.
/// </summary>
public class RecipeSitesTests
{
    private const string Header =
        "| slug | name | home | read by | search | checked | signals |\n|---|---|---|---|---|---|---|\n";

    /// <summary>The table holds all 45 sites, 13 read from their recipe data and 32 from their text.</summary>
    [Test]
    public void TheTable_HasFortyFiveSites_ThirteenReadByDataAndThirtyTwoByText()
    {
        Assert.That(RecipeSites.All, Has.Count.EqualTo(45));
        Assert.That(RecipeSites.All.Count(s => s.ReadBy == RecipeReadBy.Data), Is.EqualTo(13));
        Assert.That(RecipeSites.All.Count(s => s.ReadBy == RecipeReadBy.Text), Is.EqualTo(32));
    }

    /// <summary>Twenty sites are searched through their own WordPress search, the other 25 through the web.</summary>
    [Test]
    public void TheTable_SearchesTwentySitesThroughWordPress_AndTwentyFiveThroughTheWeb()
    {
        Assert.That(RecipeSites.All.Count(s => s.Search == RecipeSearch.WordPress), Is.EqualTo(20));
        Assert.That(RecipeSites.All.Count(s => s.Search == RecipeSearch.Web), Is.EqualTo(25));
    }

    /// <summary>Food &amp; Home lives at its www address, and we read its data and use its own search.</summary>
    [Test]
    public void FoodAndHome_IsReadByData_AndSearchedThroughWordPress()
    {
        var site = RecipeSites.Find("foodandhome")!;

        Assert.That(site.Home, Is.EqualTo(new Uri("https://www.foodandhome.co.za")));
        Assert.That((site.ReadBy, site.Search), Is.EqualTo((RecipeReadBy.Data, RecipeSearch.WordPress)));
    }

    /// <summary>Rooi Rose is read as text, lives under a path, and keeps the content signal its pages gave.</summary>
    [Test]
    public void RooiRose_IsReadByText_AndKeepsWhatItsPagesSaid()
    {
        var site = RecipeSites.Find("rooirose")!;

        Assert.That(site.Home, Is.EqualTo(new Uri("https://novanews.co.za/rooirose")));
        Assert.That(site.ReadBy, Is.EqualTo(RecipeReadBy.Text));
        Assert.That(site.Signals, Is.EqualTo("Content-Signal: ai-input=yes, ai-train=no"));
    }

    /// <summary>The Daley Plate has no recipe data and its own search is off limits, so it uses the web.</summary>
    [Test]
    public void TheDaleyPlate_IsReadByText_AndSearchedThroughTheWeb()
    {
        var site = RecipeSites.Find("thedaleyplate")!;

        Assert.That((site.ReadBy, site.Search), Is.EqualTo((RecipeReadBy.Text, RecipeSearch.Web)));
    }

    /// <summary>The Lazy Makoti is read as text, and its own search works.</summary>
    [Test]
    public void TheLazyMakoti_IsReadByText_AndSearchedThroughWordPress()
    {
        var site = RecipeSites.Find("thelazymakoti")!;

        Assert.That((site.ReadBy, site.Search), Is.EqualTo((RecipeReadBy.Text, RecipeSearch.WordPress)));
    }

    /// <summary>No two sites share a slug, slugs are lower case, and every home is a full https address.</summary>
    [Test]
    public void EverySlug_IsUniqueAndLowerCase_AndEveryHomeAnAbsoluteHttpsAddress()
    {
        var slugs = RecipeSites.All.Select(s => s.Slug).ToList();

        Assert.That(slugs, Is.Unique);
        Assert.That(slugs, Is.All.Matches<string>(s => s == s.ToLowerInvariant()));
        Assert.That(RecipeSites.All.Select(s => s.Home),
            Is.All.Matches<Uri>(u => u.IsAbsoluteUri && u.Scheme == "https"));
    }

    /// <summary>
    /// Every site was checked on 4 November 2026, and a site keeps a note only where its pages gave one.
    /// </summary>
    [Test]
    public void EverySite_WasCheckedOnTheSameDay_AndKeepsOnlyWhatItSaw()
    {
        Assert.That(RecipeSites.All.Select(s => s.Checked), Is.All.EqualTo(new DateOnly(2026, 11, 4)));
        Assert.That(RecipeSites.Find("pnp")!.Signals, Is.EqualTo("Crawl-delay: 10"));
        Assert.That(RecipeSites.Find("foodandhome")!.Signals, Is.Null);
    }

    /// <summary>You find a site by its slug whatever the case, and a slug that is no site gives nothing.</summary>
    [Test]
    public void FindingASite_IgnoresTheCase_AndAnUnknownSlugIsNull()
    {
        Assert.That(RecipeSites.Find("FoodAndHome")?.Slug, Is.EqualTo("foodandhome"));
        Assert.That(RecipeSites.Find("nosuchsite"), Is.Null);
    }

    /// <summary>A row with all seven cells becomes the site it describes.</summary>
    [Test]
    public void AWellFormedRow_IsReadWithAllItsCells()
    {
        var rows = RecipeSites.Parse(Header + "| a | A Site | https://a.example | data | web | 2026-11-04 | note |\n");

        Assert.That(rows.Single(), Is.EqualTo(new RecipeSite("a", "A Site", new Uri("https://a.example"),
            RecipeReadBy.Data, RecipeSearch.Web, new DateOnly(2026, 11, 4), "note")));
    }

    /// <summary>A row that is short of a column is an error that names the row, not a site that goes missing.</summary>
    [Test]
    public void ARowWithAMissingColumn_ThrowsWithItsNumber()
    {
        string table = Header +
                       "| a | A | https://a.example | data | web | 2026-11-04 | |\n" +
                       "| b | B | https://b.example | data |\n";

        Assert.That(() => RecipeSites.Parse(table), Throws.TypeOf<FormatException>().With.Message.Contains("Row 2"));
    }

    /// <summary>A cell that says nothing we know, like "sometimes" for how a site is read, names its row.</summary>
    [Test]
    public void ARowWithAValueThatNamesNothing_ThrowsWithItsNumber()
    {
        string table = Header + "| a | A | https://a.example | sometimes | web | 2026-11-04 | |\n";

        Assert.That(() => RecipeSites.Parse(table), Throws.TypeOf<FormatException>().With.Message.Contains("Row 1"));
    }
}
