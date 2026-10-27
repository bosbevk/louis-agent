using System.ComponentModel;

namespace LouisAgent.Choices;

/// <summary>
/// This project's own texts (ADR 0009: a text is a member of a message enum, its <c>[Description]</c> a template with named holes). Its own, since it references nothing and so cannot use core's
/// <c>MessageText</c>; <see cref="ChoiceText"/> fills the holes.
/// </summary>
public enum ChoiceMessage
{
    [Description("{Value} is not a member of {Enum}, so it has no wire name.")]
    ValueNotDefined,
}
