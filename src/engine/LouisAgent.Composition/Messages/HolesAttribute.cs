namespace LouisAgent.Composition.Messages;

/// <summary>
/// Names the record that fills a message's holes, over its member: <c>[Holes(typeof(EndWithToolHoles))]</c>. The
/// record's properties are the template's holes, by name, so a test can hold the two equal.
/// </summary>
/// <remarks>
/// A marker, as <c>[Description]</c> holds the template: the member says which record goes with it, so a misspelt hole
/// fails a test instead of throwing when the message is first sent (ADR 0119).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0119-declare-name-then-pass.md">ADR 0119: named types, declared, then passed</seealso>
[AttributeUsage(AttributeTargets.Field)]
public sealed class HolesAttribute(Type record) : Attribute
{
    /// <summary>The record whose properties fill the holes.</summary>
    public Type Record { get; } = record;
}
