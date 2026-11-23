namespace LouisAgent.Composition.Packs.Sequences;

/// <summary>
/// What a sequence does when one of its steps fails: its address is refused, its server can't be reached, it runs out
/// of time, or it answers nothing.
/// </summary>
/// <remarks>
/// An identity sets it in code and nothing writes or reads it as text, so it carries no wire names (ADR 0024 asks for
/// them only where a choice crosses a wire).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum StepFailure
{
    /// <summary>Stop there, and say why and which steps finished. What a sequence does unless it says.</summary>
    Stop,

    /// <summary>Ask the step once more, and stop as above if it fails again.</summary>
    RetryOnce,

    /// <summary>Leave the step out and carry on; the answer names every step left out.</summary>
    Skip,
}
