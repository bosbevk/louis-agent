using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Testing.Messages.Holes;

namespace LouisAgent.Testing.Messages;

/// <summary>What the shared test library says (ADR 0009: a text is a member of a message enum).</summary>
public enum TestingText
{
    [Description("No solution file (*.sln or *.slnx) sits at or above {Folder}, so the repository's root is unknown.")]
    [Holes(typeof(NoSolutionAboveHoles))]
    NoSolutionAbove
}
