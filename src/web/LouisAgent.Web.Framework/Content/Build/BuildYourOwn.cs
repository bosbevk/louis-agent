using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content.Build;

/// <summary>
/// What Build your own tells: an identity, a pack and a site of your own, each in the few steps its guide walks
/// through (<c>build-an-identity.md</c>, <c>build-a-pack.md</c>, <c>spawn-a-site.md</c>), so a reader sees the whole
/// path before opening the guide.
/// </summary>
/// <remarks>
/// The page's steps as data, so the page only configures the framework's parts (ADR 0125); the page's test holds
/// every folder the steps name to the disk.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public static class BuildYourOwn
{
    /// <summary>The folders the steps name, each one the test finds on the disk.</summary>
    public static readonly IReadOnlyList<string> Folders =
    [
        "demos/puls/identity/LouisAgent.Identities.Puls",
        "demos/puls/hosts/LouisAgent.HostPacks",
        "packs/tools/LouisAgent.Packs.WhereToBuy",
        "demos/starter/web/LouisAgent.Starter",
        "demos/louis-agent/hosts/LouisAgent.CodingApi"
    ];

    /// <summary>An identity, from a new project to an agent that answers.</summary>
    public static readonly IReadOnlyList<Step> Identity =
    [
        new("Make the project.",
            "A class library under `demos/{name}/identity/`, with its prompts embedded the way a pack's are."),
        new("Write who it is.",
            "An identity class with its name and the packs it uses, then its mode as markdown. The voice says how it " +
            "talks, and the routing says which work goes where."),
        new("Add a specialist, if the work needs one.",
            "A role of its own that the agent can ask by its address, with the packs that role uses."),
        new("Register it, test it and run it.",
            "Name it in the composition root, `demos/puls/hosts/LouisAgent.HostPacks`, then load it with " +
            "`IDENTITIES`. `demos/puls/identity/LouisAgent.Identities.Puls` is a real one to read beside the guide.")
    ];

    /// <summary>A pack, from a new project to a tool the model calls.</summary>
    public static readonly IReadOnlyList<Step> Pack =
    [
        new("Make the project.", "A class library in `packs/tools/`, which references no other pack."),
        new("Write the tools.",
            "Each public method on a tool class is a tool the model can call. Its result says what went wrong and " +
            "what to try instead."),
        new("Write the guide.",
            "A short markdown file that tells the model which tool to use, and when. It loads only in a session " +
            "that has the toolset."),
        new("Register it, test it and try it.",
            "Name it in your program's catalogue, as the coding agent's `CodingCatalog` does, then try it in a chat. " +
            "`packs/tools/LouisAgent.Packs.WhereToBuy` is the closest real one, with one tool and no settings.")
    ];

    /// <summary>A site, from the template to a page with the chat in it.</summary>
    public static readonly IReadOnlyList<Step> Site =
    [
        new("Make it from the template.",
            "`dotnet new install demos/starter/web/LouisAgent.Starter`, then " +
            "`dotnet new louis-site -n Acme -o demos/acme/web/acme`. It comes with a chat, an admin and a landing page."),
        new("Add it to the solution.",
            "Put the project in `louis-agent-solution.sln` under `demos/acme/web`, since a test holds the solution's " +
            "tree to the disk."),
        new("Serve it from a program.",
            "Copy the small program in `demos/louis-agent/hosts/LouisAgent.CodingApi`, name your identities and " +
            "packs in it, and reference your site."),
        new("Make it yours.",
            "Your colours, your themes, your landing page and your words. The framework keeps the rest, so a fix " +
            "there reaches your site too.")
    ];
}
