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

    /// <summary>A site covers its own host with or without <c>www.</c>, and nothing else.</summary>
    [TestCase("https://www.foodandhome.co.za/recipes/pork-bobotie", true)]
    [TestCase("https://foodandhome.co.za/recipes/pork-bobotie", true)]
    [TestCase("https://www.foodandhome.co.za", true)]
    [TestCase("https://notfoodandhome.co.za/recipes/pork-bobotie", false)]
    [TestCase("https://foodandhome.co.za.example.com/recipes", false)]
    public void ASite_CoversItsOwnHost(string address, bool covered) =>
        Assert.That(RecipeSites.Find("foodandhome")!.Covers(new Uri(address)), Is.EqualTo(covered));

    /// <summary>
    /// A site with a path in its home covers only what's under it: Rooi Rose is one section of its publisher's site,
    /// and a section whose name only starts the same way isn't it.
    /// </summary>
    [TestCase("https://novanews.co.za/rooirose", true)]
    [TestCase("https://www.novanews.co.za/rooirose/hoenderpasteitjies/", true)]
    [TestCase("https://novanews.co.za/", false)]
    [TestCase("https://novanews.co.za/nuus/hoender", false)]
    [TestCase("https://novanews.co.za/rooirose-extra/hoender", false)]
    public void ASiteWithAPathInItsHome_CoversOnlyWhatIsUnderIt(string address, bool covered) =>
        Assert.That(RecipeSites.Find("rooirose")!.Covers(new Uri(address)), Is.EqualTo(covered));

    /// <summary>The table holds all 46 sites, 14 read from their recipe data and 32 from their text.</summary>
    [Test]
    public void TheTable_HasFortySixSites_FourteenReadByDataAndThirtyTwoByText()
    {
        Assert.That(RecipeSites.All, Has.Count.EqualTo(46));
        Assert.That(RecipeSites.All.Count(s => s.ReadBy == RecipeReadBy.Data), Is.EqualTo(14));
        Assert.That(RecipeSites.All.Count(s => s.ReadBy == RecipeReadBy.Text), Is.EqualTo(32));
    }

    /// <summary>Best Recipes has pap en sous, as whole recipe data; the other sites only use other names.</summary>
    [Test]
    public void BestRecipes_IsInTheTable_ReadByDataThroughItsOwnSearch()
    {
        var site = RecipeSites.Find("bestrecipes")!;

        Assert.That(site.Covers(new Uri("https://www.bestrecipes.co.za/pap-en-sous-recipe/")), Is.True);
        Assert.That((site.ReadBy, site.Search), Is.EqualTo((RecipeReadBy.Data, RecipeSearch.WordPress)));
    }

    /// <summary>21 sites are searched through their own WordPress search, the other 25 through the web.</summary>
    [Test]
    public void TheTable_SearchesTwentyOneSitesThroughWordPress_AndTwentyFiveThroughTheWeb()
    {
        Assert.That(RecipeSites.All.Count(s => s.Search == RecipeSearch.WordPress), Is.EqualTo(21));
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
    /// Every site says the day it was checked, 4 November 2026 for the first ones, and a site keeps a note only where
    /// its pages gave one.
    /// </summary>
    [Test]
    public void EverySite_SaysWhenItWasChecked_AndKeepsOnlyWhatItSaw()
    {
        Assert.That(RecipeSites.All.Select(s => s.Checked), Is.All.GreaterThanOrEqualTo(new DateOnly(2026, 11, 4)));
        Assert.That(RecipeSites.Find("bestrecipes")!.Checked, Is.EqualTo(new DateOnly(2026, 11, 5)));
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
