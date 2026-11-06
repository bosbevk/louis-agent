using System.Reflection;
using LouisAgent.Web.Pages.Admin;

namespace LouisAgent.Web.Pages;

/// <summary>
/// Where the framework's pages are, for a site's router: a router only looks in the assemblies it's told about, so
/// without this a framework page would answer nothing.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class FrameworkPages
{
    /// <summary>The list a site's router takes as its <c>AdditionalAssemblies</c>.</summary>
    public static readonly Assembly[] Assemblies = [typeof(Screen).Assembly];
}
