namespace LouisAgent.Site.Resumes.Basics;

/// <summary>One of Louis's profiles elsewhere (F142): the network, his name on it, and the link.</summary>
/// <param name="Network">The network, such as LinkedIn.</param>
/// <param name="Url">The link to the profile.</param>
/// <param name="Username">His name on the network.</param>
public sealed record ResumeProfile(string Network, string Url, string? Username = null);
