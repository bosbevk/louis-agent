namespace LouisAgent.Core.Agent.Catalog.Imports;

/// <summary>
/// One uploaded file: the slot it was given for, the name it had on the person's computer, and its bytes. The host caps
/// its size before it gets here.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
public sealed record ImportFile(string Slot, string FileName, byte[] Content);
