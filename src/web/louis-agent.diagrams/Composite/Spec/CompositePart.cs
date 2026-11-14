namespace LouisAgent.Diagrams.Composite.Spec;

/// <summary>
/// One part of a class in a composite structure diagram (F134.13): a piece the class is built from, by the role
/// it plays there.
/// </summary>
/// <param name="Id">Its id, by which a port or a connector names it.</param>
/// <param name="Type">Its type, as the code has it.</param>
/// <param name="Role">The role it plays in the class: the name the class holds it by.</param>
/// <param name="Note">A line of what it does.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record CompositePart(string Id, string Type, string? Role = null, string? Note = null);
