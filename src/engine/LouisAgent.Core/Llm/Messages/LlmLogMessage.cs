using LouisAgent.Composition.Messages;
using LouisAgent.Core.Llm.Messages.Holes.Failures;
using System.ComponentModel;

namespace LouisAgent.Core.Llm.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger).

/// <summary>Log lines for the model's requests, written to the console and the run file.</summary>
public enum LlmLogMessage
{
    [Description("The model was busy before it said anything ({Failure}); asking again in {Wait} ms, " +
                 "try {Try} of {Most}")]
    [Holes(typeof(ModelAskedAgainHoles))]
    ModelAskedAgain,
}
