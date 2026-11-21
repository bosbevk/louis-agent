namespace LouisAgent.Core.Skills.Roles;

/// <summary>A role that can't be honoured (F21): a name that isn't valid, or one with no file.</summary>
public sealed class RoleException(string message) : Exception(message);
