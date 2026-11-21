namespace LouisAgent.Packs.ShopPrices.Prices.StatsSa.Sheets;

/// <summary>
/// What the two spreadsheets held: every price as a row, and the problems that kept a file or a cell from being read,
/// each naming its file, for the upload's preview.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
public sealed record StatsSaRows(IReadOnlyList<StatsSaRow> Rows, IReadOnlyList<string> Problems);
