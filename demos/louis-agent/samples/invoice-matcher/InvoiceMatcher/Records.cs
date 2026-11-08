namespace InvoiceMatcher;

/// <summary>An invoice we sent: its number, who it is for and what they owe.</summary>
public sealed record Invoice(string Number, string Customer, decimal Amount);

/// <summary>A payment from the bank statement: the reference the customer typed, and the amount.</summary>
public sealed record Payment(string Reference, decimal Amount);

/// <summary>A payment and the invoice it pays.</summary>
public sealed record Match(Invoice Invoice, Payment Payment);

/// <summary>What matched, and the payments left for a person to look at.</summary>
public sealed record MatchResult(IReadOnlyList<Match> Matched, IReadOnlyList<Payment> Unmatched);
