namespace LouisAgent.Diagrams.Composite.Spec;

/// <summary>
/// One connector in a composite structure diagram (F134.13): two parts of the class that work together, written
/// from the part that uses the other.
/// </summary>
/// <param name="From">The part that uses the other.</param>
/// <param name="To">The part it uses.</param>
/// <param name="Word">A few words on the line, when it has a name.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record CompositeConnector(string From, string To, string? Word = null);
