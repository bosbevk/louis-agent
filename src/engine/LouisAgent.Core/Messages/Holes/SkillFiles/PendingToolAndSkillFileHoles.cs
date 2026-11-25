namespace LouisAgent.Core.Messages.Holes.SkillFiles;

/// <summary>A name that both a waiting tool and a waiting skill file have, with each one's file.</summary>
public sealed record PendingToolAndSkillFileHoles(string Name, string Tool, string File);
