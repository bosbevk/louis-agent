using System.Reflection;

namespace LouisAgent.Web.Evals;

/// <summary>The library's assembly, which holds the eval pages a site's router is handed.</summary>
public static class EvalsLibrary
{
    /// <summary>Where the eval pages are.</summary>
    public static readonly Assembly Assembly = typeof(EvalsLibrary).Assembly;
}
