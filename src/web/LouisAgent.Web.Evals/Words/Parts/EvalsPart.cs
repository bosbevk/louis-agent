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

    /// <summary>One tool call, as a turn's fold counts them.</summary>
    ToolCall,

    /// <summary>More tool calls than one.</summary>
    ToolCalls,

    /// <summary>A figure the run can't give, such as a cost with no price.</summary>
    Unknown,

    /// <summary>An eval that has never been run.</summary>
    NotRunYet,

    /// <summary>A run that stopped before its end.</summary>
    CouldNotFinish,

    /// <summary>The card's button that starts a free replay (F126.3.4).</summary>
    ReplayFree,

    /// <summary>The card's button that opens the ask before a live run.</summary>
    RunLiveAsk,

    /// <summary>Why a card with no live run offers no replay.</summary>
    NothingToReplay,

    /// <summary>The ask's question, with the eval's name.</summary>
    AskQuestion,

    /// <summary>What a live run plays, with the eval's conversations.</summary>
    AskWhat,

    /// <summary>The name of the repeats' choice.</summary>
    RepeatsLabel,

    /// <summary>The estimate, with the money and the repeats chosen.</summary>
    Estimate,

    /// <summary>What the ask says when there is no estimate.</summary>
    CostNotKnown,

    /// <summary>The ask's button with the estimate in it.</summary>
    RunLiveAbout,

    /// <summary>The ask's button when there is no estimate.</summary>
    RunLive,

    /// <summary>The ask's button that closes it.</summary>
    DontRun,

    /// <summary>The words before the going eval's name, above the cards.</summary>
    GoingBefore,

    /// <summary>The words after the going eval's name.</summary>
    GoingAfter,

    /// <summary>When a going run started, on its card.</summary>
    GoingSince,

    /// <summary>The link from a going run's card to its page.</summary>
    FollowTheRun,

    /// <summary>A card whose last run a person stopped.</summary>
    Stopped
}
