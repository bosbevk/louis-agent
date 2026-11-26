namespace LouisAgent.Core.Skills.Headers.Agent.Running;

/// <summary>What a skill's run gave back: the text the model reads, and whether the run succeeded.</summary>
/// <param name="Succeeded">The script ended cleanly, in time.</param>
/// <param name="Text">What it printed, or why it failed.</param>
public sealed record SkillAnswer(bool Succeeded, string Text);
