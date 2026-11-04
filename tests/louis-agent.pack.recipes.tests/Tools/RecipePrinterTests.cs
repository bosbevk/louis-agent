using LouisAgent.Packs.Recipes.Recipes;
using LouisAgent.Packs.Recipes.Tools;

namespace LouisAgent.Packs.Recipes.Tests.Tools;

/// <summary>
/// Checks how a recipe is printed for the model: numbered lines it can quote, a sub-heading for each section, and
/// only the parts the page gave.
/// </summary>
public class RecipePrinterTests
{
    private static readonly Uri Page = new("https://example.co.za/malva");

    /// <summary>A recipe with only a name, one line and one step, and whatever else the test sets.</summary>
    private static Recipe Bare(string? yield = null, TimeSpan? prep = null, TimeSpan? cook = null,
        TimeSpan? total = null, Uri? image = null, string? author = null) => new(
        "Malva pudding", yield, prep, cook, total, ["1 egg"], [new RecipeStep(null, "Bake it.")], image, author, Page);

    /// <summary>A recipe with everything: times, two sections, a photo and an author.</summary>
    [Test]
    public void ARecipeWithEverything_IsPrintedInFullAndInOrder()
    {
        var recipe = new Recipe("Malva pudding", "8", TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(45),
            TimeSpan.FromMinutes(75), ["1 cup sugar", "1 egg"],
            [new RecipeStep("Pudding", "Mix."), new RecipeStep("Pudding", "Bake."), new RecipeStep("Sauce", "Heat.")],
            new Uri("https://img.example/a.jpg"), "Ouma", Page);

        Assert.That(RecipePrinter.Print(recipe), Is.EqualTo(
            """
                # Malva pudding
                Makes 8 · prep 30 min · cook 45 min · total 75 min

                ## Ingredients
                1. 1 cup sugar
                2. 1 egg

                ## Method
                ### Pudding
                1. Mix.
                2. Bake.
                ### Sauce
                3. Heat.

                ![Malva pudding](https://img.example/a.jpg)
                Author: Ouma
                Link: https://example.co.za/malva
                """.ReplaceLineEndings("\n")));
    }

    /// <summary>A recipe whose page gave no times, no photo, no author and no sections prints none of them.</summary>
    [Test]
    public void ARecipeWithOnlyTheBasics_PrintsNoSectionTimesPhotoOrAuthor() =>
        Assert.That(RecipePrinter.Print(Bare()), Is.EqualTo(
            """
                # Malva pudding

                ## Ingredients
                1. 1 egg

                ## Method
                1. Bake it.

                Link: https://example.co.za/malva
                """.ReplaceLineEndings("\n")));

    /// <summary>Only the parts of the line the page gave are printed, and the yield is what it makes.</summary>
    [TestCase("6 servings", null, null, null, "Makes 6 servings")]
    [TestCase(null, 20, null, null, "prep 20 min")]
    [TestCase(null, null, 90, null, "cook 90 min")]
    [TestCase("10 pies", null, 30, 50, "Makes 10 pies · cook 30 min · total 50 min")]
    [TestCase(null, 0, null, null, null)]
    public void TheTimeLine_HasOnlyThePartsThePageGave(string? yield, int? prep, int? cook, int? total,
        string? line)
    {
        static TimeSpan? Minutes(int? minutes) => minutes is { } m ? TimeSpan.FromMinutes(m) : null;

        string[] lines = RecipePrinter.Print(Bare(yield, Minutes(prep), Minutes(cook), Minutes(total))).Split('\n');

        Assert.That(lines[1], line is null ? Is.Empty : Is.EqualTo(line));
    }

    /// <summary>The page never says "serves", so the print never does.</summary>
    [Test]
    public void ThePrint_NeverSaysServes() =>
        Assert.That(RecipePrinter.Print(Bare("4")), Does.Not.Contain("Serves"));

    /// <summary>Steps count straight on through the sections, so a step can be quoted by one number.</summary>
    [Test]
    public void TheSteps_AreNumberedStraightOnThroughTheSections()
    {
        var recipe = Bare() with
        {
            Steps = [new RecipeStep("A", "One."), new RecipeStep("B", "Two."), new RecipeStep("A", "Three.")]
        };

        Assert.That(RecipePrinter.Print(recipe),
            Does.Contain("### A\n1. One.\n### B\n2. Two.\n### A\n3. Three."));
    }

    /// <summary>More than 60 ingredient lines or steps are cut at 60, and the print says how many there were.</summary>
    [Test]
    public void MoreThanSixtyLines_AreCut_AndTheCutIsSaid()
    {
        var recipe = Bare() with
        {
            Ingredients = [.. Enumerable.Range(1, 70).Select(n => $"ingredient {n}")],
            Steps = [.. Enumerable.Range(1, 65).Select(n => new RecipeStep(null, $"step {n}"))]
        };

        string print = RecipePrinter.Print(recipe);

        Assert.That(print, Does.Contain("60. ingredient 60\n(The page lists 70 ingredients; the first 60 are shown.)"));
        Assert.That(print, Does.Not.Contain("ingredient 61"));
        Assert.That(print, Does.Contain("60. step 60\n(The page lists 65 steps; the first 60 are shown.)"));
        Assert.That(print, Does.Not.Contain("step 61"));
    }

    /// <summary>Exactly 60 lines are all shown, with no cut.</summary>
    [Test]
    public void SixtyLines_AreNotCut()
    {
        var recipe = Bare() with { Ingredients = [.. Enumerable.Range(1, 60).Select(n => $"ingredient {n}")] };

        Assert.That(RecipePrinter.Print(recipe), Does.Contain("60. ingredient 60").And.Not.Contain("The page lists"));
    }
}
