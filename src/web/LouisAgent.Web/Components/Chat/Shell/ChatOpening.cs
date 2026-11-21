namespace LouisAgent.Web.Components.Chat.Shell;

/// <summary>What the frame knows when a new chat opens: its voice, where the person is, and their time zone.</summary>
public sealed record ChatOpening(string Role, string? Location, string? TimeZone);
