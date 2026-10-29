using System.Security.Cryptography;
using System.Text;
using LouisAgent.Evals.Grading.Human;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading.Judges;

/// <summary>
/// The judge's prompt (F18 step 6.2): the markdown file <c>Prompts/judges/meal-for-people.md</c>, embedded in the assembly (ADR 0011: a prompt is content, not code). Its hash is of
/// the file's bytes, written with every grade it gave, so a grade made with another prompt is another measure.
/// </summary>
public static class MealForPeoplePrompt
{
    private static readonly byte[] Bytes = Read();

    public static string Text { get; } = new UTF8Encoding(false).GetString(Bytes);

    /// <summary>What a person is asked to review (F18 step 6a): the prompt's own title line as a question, so the judge and the person answer the same words.</summary>
    public static string Question { get; } = ReviewQuestion.From(Text);

    /// <summary>The first 12 hex digits of the SHA-256 of the prompt's text: as long as a prompt hash is anywhere else in the ledger and the portal.</summary>
    public static string Hash { get; } = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Text)))[..12];

    private static byte[] Read()
    {
        var assembly = typeof(MealForPeoplePrompt).Assembly;
        string name = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith("meal-for-people.md", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return bytes.ToArray();
    }
}
