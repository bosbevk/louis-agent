using LouisAgent.Choices;

namespace LouisAgent.DocDiagrams.Tests.Projects;

/// <summary>
/// What kind of project a design doc is about, as the first words of its <c>**Kind:**</c> line say it (CL65.5).
/// The kind decides the least a doc must show: a host where it runs, a web project what its main part is made of.
/// </summary>
/// <remarks>
/// A choice from a fixed set is an enum, read once with <see cref="SettingChoice"/> by the words a person writes
/// (ADR 0024). The list is Louis's floor of 2026-11-14 with three more: a library with no path of its own, the
/// test projects' doc and a doc of content, since two docs in the projects' folders are no single project.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum ProjectKind
{
    /// <summary>A class library with a main path through it.</summary>
    [WireName("library")] Library,

    /// <summary>A library of contracts or content, with no path of its own.</summary>
    [WireName("contracts library")] ContractsLibrary,

    /// <summary>A library a program adds to serve the agent, such as the API.</summary>
    [WireName("host")] Host,

    /// <summary>A console app that composes and runs.</summary>
    [WireName("program")] Program,

    /// <summary>A Blazor WebAssembly app.</summary>
    [WireName("web app")] WebApp,

    /// <summary>A Razor class library.</summary>
    [WireName("Razor library")] RazorLibrary,

    /// <summary>A developer's tool, run by hand.</summary>
    [WireName("console tool")] ConsoleTool,

    /// <summary>The doc of the test projects.</summary>
    [WireName("test projects")] TestProjects,

    /// <summary>Files an agent reads, such as runbooks.</summary>
    [WireName("content")] Content
}
