namespace LouisAgent.Puls.Admin.Charts.Guides;

/// <summary>
/// A chart's hover note: what the measure shows, what good and bad look like, why it matters, and which way is better.
/// </summary>
public sealed record ChartGuide(string What, string Good, string Bad, string Why, string Better);
