using LouisAgent.Packs.Calculator.Sums;

namespace LouisAgent.Packs.Calculator.Tests.Sums;

/// <summary>Plain arithmetic, worked out exactly enough to cook or shop by, and nothing but arithmetic.</summary>
public class ArithmeticTests
{
    /// <summary>The sums a recipe's scaling needs, a fraction among them, with the usual order of operations.</summary>
    [TestCase("500*3/2", "750")]
    [TestCase("1*2/3", "0.6667")]
    [TestCase("2+3*4", "14")]
    [TestCase("(2+3)*4", "20")]
    [TestCase("1.5*2", "3")]
    [TestCase("0.1+0.2", "0.3")]
    [TestCase("  75 + 27 + 12 ", "114")]
    [TestCase("-2+5", "3")]
    [TestCase("100000*100000", "10000000000")]
    public void ASum_IsWorkedOut(string sum, string result) =>
        Assert.That(Arithmetic.TryWorkOut(sum, out string answer) ? answer : "refused", Is.EqualTo(result));

    /// <summary>What can't be worked out is refused, with no result to mistake for one.</summary>
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("1/0")]
    [TestCase("2+")]
    [TestCase("(2+3")]
    [TestCase("1,5*2")]
    [TestCase("⅔*3")]
    public void ASumThatCantBeWorkedOut_IsRefused(string sum) =>
        Assert.That(Arithmetic.TryWorkOut(sum, out _), Is.False);

    /// <summary>It's a calculator and no more, though the evaluator underneath does strings and functions too.</summary>
    [TestCase("Len('abc')")]
    [TestCase("IIF(1=1,2,3)")]
    [TestCase("Convert(1,'System.String')")]
    [TestCase("'a'+'b'")]
    [TestCase("1=1")]
    [TestCase("5%3")]
    public void AnythingButArithmetic_IsRefused(string sum) =>
        Assert.That(Arithmetic.TryWorkOut(sum, out _), Is.False);

    /// <summary>A sum the length of a page isn't a sum, and isn't given a parser to chew on.</summary>
    [Test]
    public void ASumTooLong_IsRefused() =>
        Assert.That(Arithmetic.TryWorkOut(string.Join('+', Enumerable.Repeat("1", 200)), out _), Is.False);
}
