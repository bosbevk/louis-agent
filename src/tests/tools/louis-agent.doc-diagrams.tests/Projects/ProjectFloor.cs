using LouisAgent.Diagrams.Sources.Types;

namespace LouisAgent.DocDiagrams.Tests.Projects;

/// <summary>
/// The least a project's doc must show, by the kind of project it is (CL65.5, Louis's pick of 2026-11-14: the
/// project's type sets the floor and its own requirements add). Every project shows its overview and its class
/// diagram; a library its main path and a sequence of it; a host or a program a sequence and where it runs; a web
/// project what its main part is made of; a tool run by hand its path.
/// </summary>
/// <remarks>
/// A table as a switch, one arm a kind, read like the guide's table, which a test holds equal to it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals</seealso>
public static class ProjectFloor
{
    /// <summary>What every project shows.</summary>
    private static readonly DiagramType[] Every = [DiagramType.Overview, DiagramType.Class];

    /// <summary>The types of diagram a kind of project asks for.</summary>
    public static IReadOnlySet<DiagramType> Of(ProjectKind kind) => kind switch
    {
        ProjectKind.Library => With(DiagramType.Activity, DiagramType.Sequence),
        ProjectKind.Host or ProjectKind.Program => With(DiagramType.Sequence, DiagramType.Deployment),
        ProjectKind.WebApp or ProjectKind.RazorLibrary => With(DiagramType.CompositeStructure),
        ProjectKind.ConsoleTool => With(DiagramType.Activity),
        ProjectKind.Content => new HashSet<DiagramType> { DiagramType.Overview },
        _ => With()
    };

    /// <summary>What a doc must show: its kind's floor, and what it names of its own.</summary>
    public static IReadOnlySet<DiagramType> Asked(ProjectDoc doc) =>
        (doc.Kind is { } kind ? Of(kind) : new HashSet<DiagramType>()).Union(doc.Own).ToHashSet();

    /// <summary>What every project shows, and these.</summary>
    private static HashSet<DiagramType> With(params DiagramType[] more) => [.. Every, .. more];
}
