using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Engine.Classes.Marks;

namespace LouisAgent.Diagrams.Tests.Classes.Marks;

/// <summary>
/// What a type is, read from the words of its kind (F134.11), so its badge can say it in one letter: the kind is
/// words a person wrote, "static, a Decorator" or "interface, step 2", and the first mark that fits is taken.
/// </summary>
public class TypeMarksTests
{
    /// <summary>A kind's words say what the type is, whatever else they say and in whatever case.</summary>
    [TestCase("interface", TypeMark.Interface, "I")]
    [TestCase("Interface, step 2", TypeMark.Interface, "I")]
    [TestCase("record", TypeMark.Record, "R")]
    [TestCase("enum, step 4", TypeMark.Enum, "E")]
    [TestCase("abstract", TypeMark.Abstract, "A")]
    [TestCase("static, a Decorator", TypeMark.Static, "S")]
    [TestCase("one per kind", TypeMark.Class, "C")]
    [TestCase(null, TypeMark.Class, "C")]
    public void AKindsWords_SayWhatTheTypeIs(string? kind, TypeMark mark, string letter)
    {
        var type = new ClassType("t", "T", kind);

        Assert.Multiple(() =>
        {
            Assert.That(TypeMarks.Of(type), Is.EqualTo(mark));
            Assert.That(TypeMarks.Letter(TypeMarks.Of(type)), Is.EqualTo(letter));
        });
    }

    /// <summary>
    /// A kind that fits two marks takes the first in the marks' order, a contract before how it is written: an
    /// abstract record is a record, and so is a static type whose kind speaks of a record.
    /// </summary>
    [TestCase("abstract record", TypeMark.Record)]
    [TestCase("static, returns a record", TypeMark.Record)]
    [TestCase("abstract, static members", TypeMark.Abstract)]
    public void AKindThatFitsTwo_TakesTheFirst(string kind, TypeMark mark) =>
        Assert.That(TypeMarks.Of(new ClassType("t", "T", kind)), Is.EqualTo(mark));

    /// <summary>An interface, a record and an enum are contracts or state, and the rest are not.</summary>
    [Test]
    public void AContract_IsAnInterfaceARecordOrAnEnum() =>
        Assert.That(Enum.GetValues<TypeMark>().Where(TypeMarks.Contract),
            Is.EqualTo(new[] { TypeMark.Interface, TypeMark.Record, TypeMark.Enum }));
}
