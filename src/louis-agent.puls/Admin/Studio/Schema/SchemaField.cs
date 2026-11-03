namespace LouisAgent.Puls.Admin.Studio.Schema;

/// <summary>
/// One setting of a check as the compose form shows it: its name in the file, what it holds, whether the check needs
/// it, and the value the check takes when it is left out.
/// </summary>
public sealed record SchemaField(string Name, FieldKind Kind, bool Required, string? Default);
