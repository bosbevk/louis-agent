using System.ComponentModel;
using LouisAgent.Composition.Messages.Holes;

namespace LouisAgent.Composition.Messages;

/// <summary>What the framework says when something's words are built with a part left out (ADR 0132).</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum WordsErrorMessage
{
    [Description("{Parts} has no words for '{Part}': give every part its words in its factory's switch.")]
    [Holes(typeof(PartWordsMissingHoles))]
    PartWordsMissing
}
