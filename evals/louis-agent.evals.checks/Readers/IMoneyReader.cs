namespace LouisAgent.Evals.Readers;

/// <summary>Reads the total an answer gives in money, such as a basket's price.</summary>
public interface IMoneyReader : IReader
{
    /// <summary>The answer's total, or null when it gives none the reader can read.</summary>
    decimal? Total(string answer);
}
