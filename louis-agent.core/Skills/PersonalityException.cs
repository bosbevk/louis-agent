namespace LouisAgent.Core.Skills;

/// <summary>A personality request that can't be honoured (F21): a name that isn't valid, or one with no file.</summary>
public sealed class PersonalityException(string message) : Exception(message);
