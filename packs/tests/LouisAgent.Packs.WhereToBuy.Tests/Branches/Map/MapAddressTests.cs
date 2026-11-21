using LouisAgent.Packs.WhereToBuy.Branches.Map;

namespace LouisAgent.Packs.WhereToBuy.Tests.Branches.Map;

/// <summary>
/// The map files an election ward where a suburb goes. Nobody shops in "Swartland Ward 10", so a ward is never the
/// place a shop is in: the next name up says where it is.
/// </summary>
public class MapAddressTests
{
    /// <summary>A ward gives way to the town, or to the city when the map names no town.</summary>
    [TestCase("Swartland Ward 10", "Malmesbury", "Swartland Local Municipality", "Malmesbury")]
    [TestCase("Swartland Ward 12", null, "Swartland Local Municipality", "Swartland Local Municipality")]
    [TestCase("Cape Town Ward 115", null, "Cape Town", "Cape Town")]
    [TestCase("ward 7", "Paarl", null, "Paarl")]
    public void AWard_IsNeverThePlace(string suburb, string? town, string? city, string expected) =>
        Assert.That(new MapAddress(Suburb: suburb, Town: town, City: city).Place, Is.EqualTo(expected));

    /// <summary>A real suburb stays, even one with Ward in its name or a number after it.</summary>
    [TestCase("Wesbank")]
    [TestCase("StellenboschCentral")]
    [TestCase("Wardville")]
    [TestCase("Edward Park")]
    [TestCase("Ward Park")]
    [TestCase("Phase 2")]
    public void ARealSuburb_IsThePlace(string suburb) =>
        Assert.That(new MapAddress(Suburb: suburb, Town: "Malmesbury").Place, Is.EqualTo(suburb));

    /// <summary>With nothing but a ward, there's no place to give.</summary>
    [Test]
    public void OnlyAWard_IsNoPlace() =>
        Assert.That(new MapAddress(Suburb: "Swartland Ward 10").Place, Is.Null);
}
