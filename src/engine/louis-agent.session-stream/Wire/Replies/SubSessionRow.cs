namespace LouisAgent.SessionStream.Wire.Replies;

/// <summary>A specialist a session keeps open: its handle, which agent, its toolsets and role.</summary>
public sealed record SubSessionRow(string Handle, string Agent, IReadOnlyList<string> Toolsets, string? Role);
