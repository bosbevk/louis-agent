using System.Text.Json.Serialization;

namespace LouisAgent.Api.Contracts.Admin.Screens;

/// <summary>One thing a screen can do: its name, its button's words, what it acts on, and the question it asks first.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record ScreenActionReply(string Name, string Label, string On, string? Confirm);

/// <summary>Asking a screen to act: <c>POST /v1/admin/screens/{name}/actions/{action}</c>, with the row if it's a row's.</summary>
public sealed record ScreenActionRequest([property: JsonPropertyName("row_id")] string? RowId);

/// <summary>What came of it: whether it happened, what to tell the person, and any field the pack refused.</summary>
public sealed record ScreenAnswerReply(bool Done, string Message, IReadOnlyDictionary<string, string> FieldErrors);
