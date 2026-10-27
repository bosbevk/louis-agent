namespace LouisAgent.Core.Config;

/// <summary>
/// The text an enum member travels as (F18 step 7.5.0, ADR 0043): <c>[WireName("fast-mode")]</c> on <c>FastMode</c>. A name that repeats in the code is an enum, but where it travels (an environment
/// variable, a role file, a request body, the protocol, a stored run) it stays the text it always was. <see cref="SettingChoice"/> parses it once where it enters and writes it back with
/// <see cref="SettingChoice.Wire{T}"/>; a member with no wire name travels as its own name in lower case.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class WireNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
