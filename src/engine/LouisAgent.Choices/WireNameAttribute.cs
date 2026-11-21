using System.Text.Json.Serialization;

namespace LouisAgent.Choices;

/// <summary>
/// The name an enum member travels as where it is text: <c>[WireName("fast-mode")] FastMode</c> (ADR 0043). It reads as what it means, and it is .NET's own
/// <see cref="JsonStringEnumMemberNameAttribute"/> underneath, so <see cref="SettingChoice"/> and the JSON writer and reader see the same name and cannot disagree.
/// A member without one travels as its own name in lower case. <see cref="SettingChoice"/> reads the base type, so someone who marks a member with .NET's attribute
/// directly still works.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class WireNameAttribute(string name) : JsonStringEnumMemberNameAttribute(name);
