using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Skills.Messages.Holes.Arguments;

namespace LouisAgent.Core.Skills.Messages;

/// <summary>
/// What a skill's tools answer the model (F97.1): a call that doesn't fit the skill's parameters, each problem named
/// so the call can be fixed in one go. Pinned in the tool-message golden file.
/// </summary>
public enum SkillToolMessage
{
    [Description("The arguments must be a JSON object, one property for each of the skill's parameters.")]
    ArgumentsNotAnObject,
    [Description("'{Parameter}' is required, and the call left it out.")]
    [Holes(typeof(RequiredArgumentLeftOutHoles))]
    RequiredArgumentLeftOut,
    [Description("'{Parameter}' isn't one of this skill's parameters, which are {Known}.")]
    [Holes(typeof(ArgumentUnknownHoles))]
    ArgumentUnknown,
    [Description("'{Parameter}' must be {Expected}, and the call gave {Given}.")]
    [Holes(typeof(ArgumentWrongTypeHoles))]
    ArgumentWrongType,
}
