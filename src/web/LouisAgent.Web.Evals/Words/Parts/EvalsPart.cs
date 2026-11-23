namespace LouisAgent.Web.Evals.Words.Parts;

/// <summary>
/// The parts of the eval pages whose words the code picks, each with its words in <see cref="EvalsWords"/>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0132-words-come-from-a-dictionary-factory.md">ADR 0132: words come from a dictionary factory</seealso>
public enum EvalsPart
{
    /// <summary>One conversation.</summary>
    Conversation,

    /// <summary>Many conversations.</summary>
    Conversations,

    /// <summary>One repeat.</summary>
    Repeat,

    /// <summary>Many repeats.</summary>
    Repeats,

    /// <summary>A mark that was met, as a screen reader says it.</summary>
    Met,

    /// <summary>A mark that wasn't met, as a screen reader says it.</summary>
    NotMet,

    /// <summary>A fact a turn didn't meet, as a failed row names its first miss.</summary>
    FactNotMet,

    /// <summary>A figure the run can't give, such as a cost with no price.</summary>
    Unknown,

    /// <summary>An eval that has never been run.</summary>
    NotRunYet,

    /// <summary>A run that stopped before its end.</summary>
    CouldNotFinish
}
