namespace LouisAgent.Diagrams.Engine.Classes.Marks;

/// <summary>
/// What a type is, as its badge says it in a class chart (F134.11): an interface, a record, an enum, abstract,
/// static, or a plain class. They stand in the order they are looked for in a type's kind, a contract first.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum TypeMark
{
    Interface,
    Record,
    Enum,
    Abstract,
    Static,
    Class
}
