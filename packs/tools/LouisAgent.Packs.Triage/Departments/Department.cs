namespace LouisAgent.Packs.Triage.Departments;

/// <summary>
/// One department the workflow runs for: the same identity and packs, with its own repository, its own error log
/// and where its people read a case raised to them. A department is configuration, never code.
/// </summary>
/// <param name="Repository">The repository its coder works in, and may never leave.</param>
/// <param name="ErrorLog">The JSON-lines log its feed reads.</param>
/// <param name="People">The folder a case raised to its people is written in.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
public sealed record Department(string Name, string Repository, string ErrorLog, string People);
