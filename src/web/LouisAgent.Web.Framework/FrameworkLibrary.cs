using System.Reflection;

namespace LouisAgent.Web.Framework;

/// <summary>
/// Where the framework's pages are: the assembly a site's router lists when the site turned the framework on.
/// </summary>
public static class FrameworkLibrary
{
    /// <summary>The library's assembly, for the router.</summary>
    public static readonly Assembly Assembly = typeof(FrameworkLibrary).Assembly;
}
