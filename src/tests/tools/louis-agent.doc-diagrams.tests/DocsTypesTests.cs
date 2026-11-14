using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Types;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.DocDiagrams.Tests;

/// <summary>
/// CL65.3, Louis: "the type of diagram must always display". Every picture of the docs that has a type shows its
/// name once, at the right of its title's line.
/// </summary>
public class DocsTypesTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>The room a picture keeps around itself, and where its title's line stands.</summary>
    private const double Pad = 24, TitleLine = 14;

    /// <summary>An attribute of a shape, as a number.</summary>
    private static double Number(XElement shape, string name) =>
        double.Parse((string?)shape.Attribute(name) ?? "NaN", CultureInfo.InvariantCulture);

    /// <summary>
    /// How many times a picture writes its type's name where it belongs: on the title's line, ending at the
    /// picture's right edge.
    /// </summary>
    private static int Shown(string json, string name)
    {
        var picture = XElement.Parse(File.ReadAllText(Path.ChangeExtension(json, ".svg")));
        double right = Number(picture, "width") - 2 * Pad;

        // Anywhere in the file, since the docs draw a picture inside a frame's group.
        return picture.Descendants(Svg + "text").Count(text => text.Value == name &&
            (string?)text.Attribute("text-anchor") == "end" &&
            Math.Abs(Number(text, "x") - right) < 0.11 && Math.Abs(Number(text, "y") - TitleLine) < 0.11);
    }

    /// <summary>Every picture with a type shows it once. One that shows none, or shows it twice, is named.</summary>
    [Test]
    public void EveryPictureWithAType_ShowsItOnce_AtTheRightOfItsTitlesLine()
    {
        var typed = DiagramSync.Find(DiagramSync.Roots(TestPaths.RepoRoot))
            .Select(json => (Json: json, Name: TypeNames.Of(DiagramReader.Read(File.ReadAllText(json)))))
            .Where(picture => picture.Name is not null).ToList();

        var wrong = typed.Where(picture => Shown(picture.Json, picture.Name!) != 1)
            .Select(picture => Path.GetRelativePath(TestPaths.RepoRoot, picture.Json));

        Assert.Multiple(() =>
        {
            Assert.That(typed, Has.Count.GreaterThan(150), "the docs' sequences, class charts and overviews at least");
            Assert.That(wrong, Is.Empty, "a picture that does not show its type once");
        });
    }
}
