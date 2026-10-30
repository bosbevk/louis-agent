using LouisAgent.Evals.Readers;

namespace LouisAgent.Evals.Checks.Tests.Readers;

/// <summary>
/// The reader catalogue: an eval file names a reader beside its key, and the catalogue finds it by that name, of the
/// kind the check needs, or refuses the name, so a typo never becomes a check that reads nothing.
/// </summary>
public class ReaderCatalogTests
{
    /// <summary>A money reader and a number reader, under two names.</summary>
    private static readonly ReaderCatalog Catalog =
        new([new FakeMoney("plain-money"), new FakeNumbers("plain-numbers")]);

    /// <summary>A reader is found by its name, as the kind the check asks for.</summary>
    [Test]
    public void AReader_IsFoundByItsName_AsTheKindAskedFor()
    {
        Assert.That(Catalog.Get<IMoneyReader>("plain-money").Name, Is.EqualTo("plain-money"));
        Assert.That(Catalog.Get<INumberReader>("plain-numbers").Name, Is.EqualTo("plain-numbers"));
    }

    /// <summary>A name the catalogue doesn't have is refused, naming the readers it has.</summary>
    [Test]
    public void AnUnknownName_IsRefused_NamingTheReadersItHas()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Catalog.Get<IMoneyReader>("rand"));

        Assert.That(refusal!.Message, Does.Contain("rand").And.Contain("plain-money").And.Contain("plain-numbers"));
    }

    /// <summary>A reader of the wrong kind is refused, naming it: a number reader can't read a total.</summary>
    [Test]
    public void AReaderOfTheWrongKind_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Catalog.Get<IMoneyReader>("plain-numbers"));

        Assert.That(refusal!.Message, Does.Contain("plain-numbers").And.Contain("MoneyReader"));
    }

    /// <summary>
    /// Two readers under one name are refused when the catalogue is built: a file couldn't tell which.
    /// </summary>
    [Test]
    public void TwoReadersUnderOneName_AreRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() =>
            _ = new ReaderCatalog([new FakeMoney("same"), new FakeNumbers("same")]));

        Assert.That(refusal!.Message, Does.Contain("same"));
    }

    /// <summary>The empty catalogue has no readers, so any name is refused.</summary>
    [Test]
    public void TheEmptyCatalogue_RefusesAnyName()
    {
        Assert.Throws<InvalidDataException>(() => ReaderCatalog.Empty.Get<IMoneyReader>("plain-money"));
    }

    /// <summary>A money reader that reads nothing, under a name.</summary>
    private sealed class FakeMoney(string name) : IMoneyReader
    {
        /// <inheritdoc />
        public string Name { get; } = name;

        /// <inheritdoc />
        public decimal? Total(string answer) => null;
    }

    /// <summary>A number reader that reads nothing, under a name.</summary>
    private sealed class FakeNumbers(string name) : INumberReader
    {
        /// <inheritdoc />
        public string Name { get; } = name;

        /// <inheritdoc />
        public IReadOnlyList<int> Numbers(string answer) => [];
    }
}
