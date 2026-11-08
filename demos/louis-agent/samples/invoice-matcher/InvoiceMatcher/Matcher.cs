namespace InvoiceMatcher;

/// <summary>Matches the bank's payments to our invoices: the invoice number in the reference, and the full amount.</summary>
public static class Matcher
{
    /// <summary>Each payment that names an invoice and pays it in full is matched; the rest are left for a person.</summary>
    public static MatchResult Match(IReadOnlyList<Invoice> invoices, IEnumerable<Payment> payments)
    {
        var matched = new List<Match>();
        var unmatched = new List<Payment>();
        foreach (var payment in payments)
        {
            var invoice = invoices.FirstOrDefault(invoice =>
                payment.Reference.Contains(invoice.Number) && invoice.Amount == payment.Amount);
            if (invoice is null)
            {
                unmatched.Add(payment);
                continue;
            }

            matched.Add(new Match(invoice, payment));
        }

        return new MatchResult(matched, unmatched);
    }
}
