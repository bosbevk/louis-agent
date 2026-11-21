using System.Reflection;

namespace LouisAgent.Web.Evals;

/// <summary>
/// Where the studio's pages are (CL57): the assembly a site's router lists, through <c>FrameworkPages.With</c>, when
/// the site opted into the studio. Without it the Evals, run, review, studio and prompts pages answer nothing.
/// </summary>
public static class EvalLibrary
{
    /// <summary>The library's assembly, for the router.</summary>
    public static readonly Assembly Assembly = typeof(EvalServices).Assembly;
}
