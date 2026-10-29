using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Puls.Admin.Store;

namespace LouisAgent.Puls.Tests.Admin.Store;

/// <summary>What the pill at the top says about the store, and whether a run may start.</summary>
public class StoreStatusTests
{
    /// <summary>A store that answers is up; one that doesn't, or none at all, is down, and says why on hover.</summary>
    [Test]
    public void ThePill_SaysWhetherTheStoreAnswers()
    {
        var up = new StoreRow(true, null);
        var down = new StoreRow(true, "timed out");
        var none = new StoreRow(false, null);

        Assert.That((StoreStatus.Up(up), StoreStatus.Label(up), StoreStatus.Title(up)),
            Is.EqualTo((true, "store", "puls-mongo answers")));
        Assert.That((StoreStatus.Up(down), StoreStatus.Label(down), StoreStatus.Title(down)),
            Is.EqualTo((false, "store down", "timed out")));
        Assert.That((StoreStatus.Up(none), StoreStatus.Label(none), StoreStatus.Title(none)),
            Is.EqualTo((false, "no store", "No store")));
    }
}
