namespace LouisAgent.Evals.Protocol.Parts.Conversations;

/// <summary>Which conversation: the mode, the repeat and the task.</summary>
/// <remarks>
/// A value used as a key (ADR 0040), shared by the runner's readers and the API's store so neither keeps its own copy
/// (a shared Data Transfer Object contract, Fowler, PoEAA). No line carries it whole: the events keep its three fields
/// flat, and a turn's key and a conversation's event hand it out.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record ConversationKey(string Mode, int Repeat, string Task);
