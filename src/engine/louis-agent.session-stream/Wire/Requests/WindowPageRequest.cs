namespace LouisAgent.SessionStream.Wire.Requests;

/// <summary>
/// The page an agent's own window on the visitor's screen sits in (FX134): a showroom's live window shows a business
/// on a mock of its website, and the guide's message carries that page, so an ask to the business sends it on.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record WindowPageRequest(string Identity, PageRequest Page);
