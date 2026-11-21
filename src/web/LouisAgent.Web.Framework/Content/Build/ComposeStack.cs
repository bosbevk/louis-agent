using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content.Build;

/// <summary>
/// One Docker stack the repo runs on your own machine: its compose file, the services in it, the command that
/// starts it, and what you get in a line or two.
/// </summary>
/// <param name="File">The compose file, from the repo's root.</param>
/// <param name="Services">The services the page names, each one the file's own.</param>
/// <param name="Command">The command that starts it, typed from the repo's root.</param>
/// <param name="What">What it gives you, in Markdown.</param>
public sealed record ComposeStack(string File, IReadOnlyList<string> Services, string Command, string What)
{
    /// <summary>The stack as a term list shows it: its file, then what it gives you.</summary>
    public Term Term => new([File], What);
}
