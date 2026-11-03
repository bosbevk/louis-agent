using LouisAgent.Choices;

namespace LouisAgent.Evals.Readers;

/// <summary>
/// What a reader reads: a money total, numbers, images or prices, one per reader interface. The studio's form offers a
/// check only the readers of the kind it takes.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum ReaderKind
{
    [WireName("money")] Money,
    [WireName("number")] Number,
    [WireName("image")] Image,
    [WireName("price")] Price,
}
