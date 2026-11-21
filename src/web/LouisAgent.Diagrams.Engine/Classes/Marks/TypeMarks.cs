using LouisAgent.Diagrams.Classes.Spec;

namespace LouisAgent.Diagrams.Engine.Classes.Marks;

/// <summary>
/// Says what a type is from the words of its kind (F134.11), so its badge can say it in one letter. A kind is
/// words a person wrote, "static, a Decorator" or "interface, step 2", so the mark is looked for in them.
/// </summary>
/// <remarks>
/// The first mark that fits is taken, in the marks' own order, a contract before how a type is written: an
/// abstract record is a record. A kind that names none of them, or no kind at all, is a plain class.
/// </remarks>
public static class TypeMarks
{
    /// <summary>The word that says each mark in a kind, in the order they are looked for.</summary>
    private static readonly (string Word, TypeMark Mark)[] Words =
    [
        ("interface", TypeMark.Interface), ("record", TypeMark.Record), ("enum", TypeMark.Enum),
        ("abstract", TypeMark.Abstract), ("static", TypeMark.Static)
    ];

    /// <summary>What a type is: the first mark its kind's words name, or a plain class.</summary>
    public static TypeMark Of(ClassType type)
    {
        if (type.Kind is not { } kind) return TypeMark.Class;

        var named = Words.Where(word => kind.Contains(word.Word, StringComparison.OrdinalIgnoreCase)).ToList();
        return named.Count > 0 ? named[0].Mark : TypeMark.Class;
    }

    /// <summary>The letter a mark's badge shows: the first of its name.</summary>
    /// <remarks>A switch on the mark, one arm a mark (ADR 0051).</remarks>
    public static string Letter(TypeMark mark) => mark switch
    {
        TypeMark.Interface => "I",
        TypeMark.Record => "R",
        TypeMark.Enum => "E",
        TypeMark.Abstract => "A",
        TypeMark.Static => "S",
        _ => "C"
    };

    /// <summary>
    /// An interface, a record and an enum are a contract or state. The rest are parts that do the work.
    /// </summary>
    public static bool Contract(TypeMark mark) => mark is TypeMark.Interface or TypeMark.Record or TypeMark.Enum;
}
