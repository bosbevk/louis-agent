using LouisAgent.Choices;

namespace LouisAgent.Evals.Readers.Listing;

/// <summary>
/// What a reader reads: a money total, numbers, images, prices or a recipe's link, one per reader interface. The
/// studio's form offers a check only the readers of the kind it takes.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum ReaderKind
{
    [WireName("money")] Money,
    [WireName("number")] Number,
    [WireName("image")] Image,
    [WireName("price")] Price,
    [WireName("link")] Link,
}
