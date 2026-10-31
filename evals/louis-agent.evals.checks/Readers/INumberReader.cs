namespace LouisAgent.Evals.Readers;

/// <summary>Reads the whole numbers an answer names, such as how many people a meal is for.</summary>
public interface INumberReader : IReader
{
    /// <summary>
    /// Every number the answer names, in the order it first says them, each once; none when it names none.
    /// </summary>
    IReadOnlyList<int> Numbers(string answer);
}
