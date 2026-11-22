using LouisAgent.Web.Components.Site.Text;
using LouisAgent.Web.Framework.Content.Build.Parts;
using LouisAgent.Composition.Words;

namespace LouisAgent.Web.Framework.Content.Build;

/// <summary>
/// What Build your own tells: an identity, a pack and a site of your own, each in the few steps its guide walks
/// through (<c>build-an-identity.md</c>, <c>build-a-pack.md</c>, <c>spawn-a-site.md</c>), so a reader sees the whole
/// path before opening the guide.
/// </summary>
/// <remarks>
/// The page's steps as data, so the page only configures the framework's parts (ADR 0125), each list a dictionary
/// factory keyed by its steps (ADR 0132); the page's test holds every folder the steps name to the disk.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0132-words-come-from-a-dictionary-factory.md">ADR 0132: words come from a dictionary factory</seealso>
public static class BuildYourOwn
{
    /// <summary>The folders the steps name, each one the test finds on the disk.</summary>
    public static readonly PartWords<ExampleFolder, string> Folders = PartWords<ExampleFolder, string>.Of(folder =>
        folder switch
        {
            ExampleFolder.CodingIdentity => "demos/louis-agent/identity/LouisAgent.Identities.Coding",
            ExampleFolder.CodingCatalog => "demos/louis-agent/hosts/LouisAgent.Coding.Catalog",
            ExampleFolder.Calculator => "packs/tools/LouisAgent.Packs.Calculator",
            ExampleFolder.Starter => "demos/starter/web/LouisAgent.Starter",
            ExampleFolder.CodingApi => "demos/louis-agent/hosts/LouisAgent.CodingApi",
            _ => throw new ArgumentOutOfRangeException(nameof(folder), folder, null)
        });

    /// <summary>An identity, from a new project to an agent that answers.</summary>
    public static readonly PartWords<IdentityStep, Step> Identity = PartWords<IdentityStep, Step>.Of(step =>
        step switch
        {
            IdentityStep.MakeTheProject => new Step("Make the project.",
                "A class library under `demos/{name}/identity/`, with its `agents/` folder embedded the way a pack's " +
                "`toolsets/` is."),
            IdentityStep.WriteWhoItIs => new Step("Write who it is.",
                "An identity class with its name and the packs it uses, then its mode as markdown in " +
                "`agents/{name}/voice.md`. The voice says how it talks, and the routing says which work goes where. " +
                "Each skill it reads is a folder beside it, `skills/{skill}/SKILL.md`."),
            IdentityStep.AddASubagent => new Step("Add a subagent, if the work needs one.",
                "A helper of its own, kept inside the agent at `agents/{name}/subagents/{helper}/voice.md`, that " +
                "the agent asks by its address, `{name}.{helper}`, with the packs it uses."),
            IdentityStep.RegisterIt => new Step("Register it, test it and run it.",
                "Name it in your program's catalogue, as `demos/louis-agent/hosts/LouisAgent.Coding.Catalog` names " +
                "the coding agent's. `demos/louis-agent/identity/LouisAgent.Identities.Coding` is a real one to read " +
                "beside the guide."),
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, null)
        });

    /// <summary>A pack, from a new project to a tool the model calls.</summary>
    public static readonly PartWords<PackStep, Step> Pack = PartWords<PackStep, Step>.Of(step => step switch
    {
        PackStep.MakeTheProject => new Step("Make the project.",
            "A class library in `packs/tools/`, which references no other pack."),
        PackStep.WriteTheTools => new Step("Write the tools.",
            "Each public method on a tool class is a tool the model can call. Its result says what went wrong and " +
            "what to try instead."),
        PackStep.WriteTheGuide => new Step("Write the guide.",
            "A short markdown file that tells the model which tool to use, and when. It loads only in a session " +
            "that has the toolset."),
        PackStep.RegisterIt => new Step("Register it, test it and try it.",
            "Name it in your program's catalogue, as the coding agent's `CodingCatalog` does, then try it in a chat. " +
            "`packs/tools/LouisAgent.Packs.Calculator` is the closest real one, with one tool and no settings."),
        _ => throw new ArgumentOutOfRangeException(nameof(step), step, null)
    });

    /// <summary>A site, from the template to a page with the chat in it.</summary>
    public static readonly PartWords<SiteStep, Step> Site = PartWords<SiteStep, Step>.Of(step => step switch
    {
        SiteStep.MakeItFromTheTemplate => new Step("Make it from the template.",
            "`dotnet new install demos/starter/web/LouisAgent.Starter`, then " +
            "`dotnet new louis-site -n Acme -o demos/acme/web/acme`. It comes with a chat, an admin and a landing " +
            "page."),
        SiteStep.AddItToTheSolution => new Step("Add it to the solution.",
            "Put the project in `louis-agent-solution.sln` under `demos/acme/web`, since a test holds the solution's " +
            "tree to the disk."),
        SiteStep.ServeIt => new Step("Serve it from a program.",
            "Copy the small program in `demos/louis-agent/hosts/LouisAgent.CodingApi`, name your identities and " +
            "packs in it, and reference your site."),
        SiteStep.MakeItYours => new Step("Make it yours.",
            "Your colours, your themes, your landing page and your words. The framework keeps the rest, so a fix " +
            "there reaches your site too."),
        _ => throw new ArgumentOutOfRangeException(nameof(step), step, null)
    });
}
