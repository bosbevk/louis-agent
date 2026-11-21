using System.Text.Json.Serialization;

namespace LouisAgent.Diagrams.Classes.Spec;

/// <summary>How one type stands to another: what kind of link it is, and a word to say more.</summary>
public sealed record ClassRelation(
    string From,
    string To,
    // Written always, though inherits is the first kind, so a person reading the file sees what the link is.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    RelationKind Kind,
    string? Word = null);
