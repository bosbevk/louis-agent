namespace InvoiceMatcher.Tests;

/// <summary>How the bank's payments find their invoices, with references the way customers really type them.</summary>
public class MatcherTests
{
    private static readonly Invoice Acme = new("INV-1042", "Acme Plumbing", 1250.00m);
    private static readonly Invoice Karoo = new("INV-104", "Karoo Farm Stall", 1250.00m);

    /// <summary>A reference that names the invoice and pays it in full matches.</summary>
    [Test]
    public void AnExactReference_Matches()
    {
        var result = Matcher.Match([Acme], [new Payment("INV-1042", 1250.00m)]);

        Assert.That(result.Matched.Single().Invoice, Is.EqualTo(Acme));
    }

    /// <summary>A payment short of the invoice is left for a person.</summary>
    [Test]
    public void APartPayment_IsLeftForAPerson()
    {
        var result = Matcher.Match([Acme], [new Payment("INV-1042", 1000.00m)]);

        Assert.That(result.Unmatched, Has.Count.EqualTo(1));
    }

    /// <summary>Customers type the number in lower case, with words around it.</summary>
    [Test]
    public void ALowerCaseReference_StillMatches()
    {
        var result = Matcher.Match([Acme], [new Payment("acme inv-1042 thanks", 1250.00m)]);

        Assert.That(result.Matched.Single().Invoice, Is.EqualTo(Acme));
    }

    /// <summary>One invoice number inside a longer one is a different invoice.</summary>
    [Test]
    public void AShorterNumber_DoesNotTakeALongerOnesPayment()
    {
        var result = Matcher.Match([Karoo, Acme], [new Payment("INV-1042", 1250.00m)]);

        Assert.That(result.Matched.Single().Invoice, Is.EqualTo(Acme));
    }
}
