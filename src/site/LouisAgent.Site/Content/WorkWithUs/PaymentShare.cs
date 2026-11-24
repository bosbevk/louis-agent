namespace LouisAgent.Site.Content.WorkWithUs;

/// <summary>One half of what a piece of work costs (F177): how much of it, and when it is paid.</summary>
/// <param name="Share">The part of the price, as the tile's figure, such as "50%".</param>
/// <param name="When">When it is paid, as the note under the figure.</param>
public sealed record PaymentShare(string Share, string When);
