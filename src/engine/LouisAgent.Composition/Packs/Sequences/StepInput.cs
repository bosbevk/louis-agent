namespace LouisAgent.Composition.Packs.Sequences;

/// <summary>
/// What each step of a sequence is given to work on. The first step always gets the first message, whichever is
/// picked.
/// </summary>
/// <remarks>
/// An identity sets it in code and nothing writes or reads it as text, so it carries no wire names (ADR 0024 asks for
/// them only where a choice crosses a wire).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum StepInput
{
    /// <summary>
    /// The first message, with the answer of the step before under it. What a sequence does unless it says.
    /// </summary>
    FirstMessageAndPreviousAnswer,

    /// <summary>The first message, then every answer so far, each under the step that gave it.</summary>
    WholeConversation,

    /// <summary>Only the answer of the step before, as it came back.</summary>
    PreviousAnswer,
}
