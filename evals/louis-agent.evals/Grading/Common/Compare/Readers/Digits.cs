using LouisAgent.Evals.Readers;

namespace LouisAgent.Evals.Grading.Common.Compare.Readers;

/// <summary>
/// Reads every whole number an answer writes in digits ("for 4 people, then 6"), in the order it first says them,
/// each once. Numbers in words ("four") aren't read: that takes a language's words, which an identity's own reader
/// knows.
/// </summary>
/// <remarks>
/// The plain number reader any eval can name, <c>digits</c>. It walks the text a character at a time, with no pattern
/// (ADR 0057), and skips a run of digits too long to be a whole number.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public sealed class Digits : INumberReader
{
    /// <inheritdoc />
    public string Name => "digits";

    /// <inheritdoc />
    public IReadOnlyList<int> Numbers(string answer)
    {
        var found = new List<int>();
        var start = -1;
        // One past the end, so a number the answer ends on is read too.
        for (var i = 0; i <= answer.Length; i++)
        {
            var digit = i < answer.Length && char.IsAsciiDigit(answer[i]);
            if (digit && start < 0) start = i;
            if (digit || start < 0) continue;

            if (int.TryParse(answer.AsSpan(start, i - start), out var number) && !found.Contains(number))
                found.Add(number);
            start = -1;
        }

        return found;
    }
}
