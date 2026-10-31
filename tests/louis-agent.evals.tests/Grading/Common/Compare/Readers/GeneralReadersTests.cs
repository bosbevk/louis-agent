using LouisAgent.Evals.Grading.Common.Compare.Readers;

namespace LouisAgent.Evals.Tests.Grading.Common.Compare.Readers;

/// <summary>
/// The harness's plain readers, the ones any eval file can name: <c>currency-amount</c> reads the last amount written
/// with a currency sign, and <c>digits</c> reads the whole numbers written in digits.
/// </summary>
public class GeneralReadersTests
{
    /// <summary>The general readers are listed under the names a file uses.</summary>
    [Test]
    public void TheGeneralReaders_AreListedByName() =>
        Assert.That(GeneralReaders.All.Select(reader => reader.Name),
            Is.EqualTo(new[] { "currency-amount", "digits", "markdown-images" }));

    /// <summary>
    /// An amount after a currency sign is read, with a space or none, and a last separator followed by two digits as
    /// the decimal mark, so both ways of writing cents read the same.
    /// </summary>
    [TestCase("Totaal: R 249,50", 249.50)]
    [TestCase("About $12.75 in all.", 12.75)]
    [TestCase("It comes to R1,234.56 at Checkers.", 1234.56)]
    [TestCase("£40", 40)]
    public void AnAmountAfterACurrencySign_IsRead(string answer, decimal amount) =>
        Assert.That(new CurrencyAmount().Total(answer), Is.EqualTo(amount));

    /// <summary>The last amount is the total: an answer lists its prices, then sums them.</summary>
    [Test]
    public void TheLastAmount_IsTheTotal() =>
        Assert.That(new CurrencyAmount().Total("Hoender R69,99, rys R20,00. Totaal R89,99."), Is.EqualTo(89.99m));

    /// <summary>
    /// An answer with no amount after a sign gives nothing, and a word that starts with R is not an amount.
    /// </summary>
    [TestCase("Rice for 4 people.")]
    [TestCase("No prices today.")]
    public void NoAmount_IsNothing(string answer) => Assert.That(new CurrencyAmount().Total(answer), Is.Null);

    /// <summary>Every whole number in digits is read, in the order first said, each once.</summary>
    [Test]
    public void EveryWholeNumber_IsRead_InOrder_EachOnce() =>
        Assert.That(new Digits().Numbers("For 4 people, then 6, then 4 again."), Is.EqualTo(new[] { 4, 6 }));

    /// <summary>An answer with no number in digits gives none.</summary>
    [Test]
    public void NoDigits_IsNone() => Assert.That(new Digits().Numbers("For four people."), Is.Empty);

    /// <summary>
    /// Every image written as <c>![alt](url)</c> is read, in order, an image inside a link too; a plain link is no
    /// image, nor is one whose address isn't a web address or holds a space.
    /// </summary>
    [Test]
    public void EveryMarkdownImage_IsRead_NotAPlainLink() =>
        Assert.That(
            new MarkdownImages().Images(
                "![Bobotie](https://a.co.za/b.jpg) and [a link](https://a.co.za/x) and " +
                "[![Dish](http://a.co.za/c.png)](https://a.co.za/y), ![local](images/d.png), " +
                "![x](https://a.co.za/e f.png)"),
            Is.EqualTo(new[] { "https://a.co.za/b.jpg", "http://a.co.za/c.png" }));

    /// <summary>
    /// An image's alt text runs to its first closing bracket, so an unfinished image before a whole one reads as the
    /// whole one, once.
    /// </summary>
    [Test]
    public void AnUnfinishedImage_ReadsTheNextWholeOne_Once() =>
        Assert.That(new MarkdownImages().Images("![a ![b](https://a.co.za/b.jpg)"),
            Is.EqualTo(new[] { "https://a.co.za/b.jpg" }));

    /// <summary>Text with no image gives none.</summary>
    [Test]
    public void NoImage_IsNone() => Assert.That(new MarkdownImages().Images("Resep."), Is.Empty);
}
