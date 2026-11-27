namespace LouisAgent.Core.Prompting.Headers.Agent;

/// <summary>
/// A skill's folder holds something the reader doesn't take (F97.1): the message names the skill and what to change.
/// </summary>
public sealed class SkillRefusedException(string message) : Exception(message);
