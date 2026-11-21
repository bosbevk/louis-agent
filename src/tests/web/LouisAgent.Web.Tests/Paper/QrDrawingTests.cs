using LouisAgent.Web.Paper;

namespace LouisAgent.Web.Tests.Paper;

/// <summary>
/// FX154, Louis: "in a pdf hyperlinks is qr codes always". A link on paper can't be clicked, so it is drawn as a
/// QR code. The drawing is plain data, a square's size and the path of its dark cells, so it is tested without a
/// browser and drawn by any component.
/// </summary>
public class QrDrawingTests
{
    /// <summary>The quiet edge kept around a code, in cells, each side.</summary>
    private const int Edge = QrDrawing.QuietEdge;

    /// <summary>A code is a square of a size the QR standard allows, with its quiet edge around it.</summary>
    [TestCase("https://louis-agent.dev/cv")]
    [TestCase("mailto:louis.marais@live.com")]
    [TestCase("https://www.linkedin.com/in/louismarais86")]
    public void ACode_IsASquare_OfASizeTheStandardAllows(string address)
    {
        var drawing = QrDrawing.Of(address);
        int cells = drawing.Size - 2 * Edge;

        Assert.Multiple(() =>
        {
            Assert.That(cells, Is.InRange(21, 177), "a QR code is 21 to 177 cells a side");
            Assert.That((cells - 17) % 4, Is.Zero, "and grows four cells at a time");
            Assert.That(drawing.Path, Does.StartWith("M"), "the dark cells, as an SVG path");
        });
    }

    /// <summary>The same address always gives the same code, and another address another code.</summary>
    [Test]
    public void TheSameAddress_GivesTheSameCode_AndAnotherAddressAnother()
    {
        var cv = QrDrawing.Of("https://louis-agent.dev/cv");

        Assert.Multiple(() =>
        {
            Assert.That(QrDrawing.Of("https://louis-agent.dev/cv"), Is.EqualTo(cv));
            Assert.That(QrDrawing.Of("https://louis-agent.dev/pricing").Path, Is.Not.EqualTo(cv.Path));
        });
    }

    /// <summary>A longer address needs a bigger code, so nothing is cut off to fit.</summary>
    [Test]
    public void ALongerAddress_NeedsABiggerCode()
    {
        string longer = "https://louis-agent.dev/framework/measured/checks?" + new string('a', 200);

        Assert.That(QrDrawing.Of(longer).Size, Is.GreaterThan(QrDrawing.Of("https://louis-agent.dev/cv").Size));
    }
}
