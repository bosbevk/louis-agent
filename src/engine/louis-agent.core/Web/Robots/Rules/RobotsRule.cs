namespace LouisAgent.Core.Web.Robots.Rules;

/// <summary>
/// One line of a <c>robots.txt</c> group: a path pattern the site lets automated readers read (<c>Allow</c>) or asks
/// them to leave alone (<c>Disallow</c>). The pattern is the text after the colon, <c>*</c> and <c>$</c> included.
/// </summary>
public sealed record RobotsRule(bool Allow, string Pattern);
