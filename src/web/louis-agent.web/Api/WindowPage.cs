using LouisAgent.Web.Chats;

namespace LouisAgent.Web.Api;

/// <summary>
/// The page an agent's window on the visitor's screen sits in (FX134): a showroom's live window shows a business on
/// a mock of its website, and the guide's message carries that page, so the guide's ask sends it on to the business.
/// </summary>
/// <remarks>Data Transfer Object (Fowler, PoEAA), as <see cref="MessageBody"/> is, the wire's camelCase.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record WindowPage(string Identity, Page Page);
