using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Packs.Calculator.Messages;
using LouisAgent.Packs.Calculator.Sums;

namespace LouisAgent.Packs.Calculator.Tools;

/// <summary>
/// The <c>calculator</c> toolset: a model hands over all its sums in one call and gets each result back beside its sum.
/// One bad sum says why and leaves the others their results, so the model only has to fix that one.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
public sealed class CalculatorTools
{
    /// <summary>The most sums one call may hold, so the answer stays bounded.</summary>
    internal const int MostSums = 60;

    /// <summary>Works out every sum it's given, one line each.</summary>
    [Description(
        "Works out sums exactly, one line per sum. Put every sum you need in one call, and use the results as they " +
        "come back instead of working anything out yourself.")]
    public string Calculate(
        [Description(
            "The sums, using only numbers, + - * / and brackets, with a point for decimals and a division for a " +
            "fraction: [\"500*3/2\", \"1*2/3\"].")]
        string[] sums)
    {
        if (sums.Length == 0) return MessageText.Format(CalculatorToolMessage.NoSums);
        if (sums.Length > MostSums)
            return MessageText.Format(CalculatorToolMessage.TooManySums, new
            {
                Most = MostSums,
                Count = sums.Length
            });

        return string.Join('\n', sums.Select(Line));
    }

    /// <summary>One sum with its result, or with why it has none.</summary>
    private static string Line(string sum) =>
        Arithmetic.TryWorkOut(sum, out string result)
            ? MessageText.Format(CalculatorToolMessage.SumWorkedOut, new
            {
                Sum = sum,
                Result = result
            })
            : MessageText.Format(CalculatorToolMessage.SumRefused, new { Sum = sum });
}
