namespace LouisAgent.Puls.Admin.Charts.Guides;

/// <summary>
/// The hover note for each measure (F31, Louis: "on hover can we explain what is good and bad and why?"): one text per
/// measure, shared by the explorer's rings and the trend's lines, so both say the same.
/// </summary>
/// <remarks>
/// A table of texts, the way Puls keeps its words (<c>PulsText</c>): the browser app can't reach core's message enums,
/// so its words live in a plain class it owns (the unattended run's finding 4). A switch expression on the measure
/// keeps one arm per measure and lets the build say when a new measure has no note.
/// </remarks>
public static class ChartGuides
{
    /// <summary>The note for one measure.</summary>
    public static ChartGuide For(ChartTopic topic) => topic switch
    {
        ChartTopic.Cost => new("What one repeat of the mode cost: Puls's own requests and the specialist's.",
            "Lower, with the same or a better score.",
            "A dearer mode that doesn't give better answers.",
            "Delegating pays only when the specialist does something better or cheaper; otherwise it's just another "
            + "model round.",
            "Lower is better."),
        ChartTopic.Tokens => new("Every token, by kind: fresh input, cache written, cache read and output.",
            "Few tokens, most of them read from the cache.",
            "Much fresh input or cache written: every request starts almost from scratch.",
            "Tokens are the cost and the wait; a token from the cache costs a tenth of a fresh one.",
            "Lower is better."),
        ChartTopic.Time => new("Seconds per turn; the ring splits the model's time between Puls and the specialist.",
            "Short turns.",
            "Long turns waiting on a specialist or on tools.",
            "The user waits every second; a specialist adds a whole model round.",
            "Lower is better."),
        ChartTopic.Routing => new("How many conversations went where this mode should send them: the cook for a meal "
                                  + "where the mode has a cook, Puls itself where it has none, and for the rest. It "
                                  + "measures the route, not whether the answer was any good: a mode with no cook "
                                  + "routes every meal to itself and scores 100% here while failing the meal.",
            "100%, all green.",
            "Any red: a question with the wrong role.",
            "Routed wrong costs money and gives a weaker answer, even when the text reads well. This measures the "
            + "route, not the answer.",
            "Higher is better."),
        ChartTopic.Accuracy => new("The shopping list's rows that are right: priced with a link to where the price "
                                   + "was found, estimated (the cook gives estimates while no shop's prices are "
                                   + "read), or rightly without a price where the user's town has no shop.",
            "Green: every price with a source; in a town with no such shop, every row without a price. Light green: "
            + "an estimate (no shop's prices are looked up, so the cook gives estimates), counted as priced.",
            "Orange (guessed, no source) or red (no price, or a price from a store that isn't the town's).",
            "A guessed price looks like an answer, but nobody can check it; only a price with a source is a real "
            + "answer.",
            "Higher is better."),
        ChartTopic.Cache => new("The share of the input read from the cache instead of sent fresh.",
            "High, 80% or more.",
            "Low: the start of the prompt changes between requests.",
            "A cache hit costs a tenth and is faster; a low share means the system prompt or the tools change along "
            + "the way.",
            "Higher is better."),
        ChartTopic.Tools => new("Tool calls that worked against those refused or failed, with the biggest reason.",
            "All green.",
            "Red: the model searched and got nothing back.",
            "A failed call costs a round, and the answer then rests on guessing (as when Tavily's limit was reached).",
            "Lower is better."),
        ChartTopic.Sources => new("The websites read: South African shops (their catalogues searched, or their "
                                  + "pages), other SA sites, maps (where a branch was looked up) and elsewhere.",
            "Green: SA shops, where the price is one you can pay.",
            "Grey: foreign sites for an SA price.",
            "A price from an SA shop is the answer; a site from elsewhere is background at best.",
            "More SA shops is better; more sites on their own is not."),
        ChartTopic.PassAt => new("pass@k: the share of scenarios that passed at least once in their k trials. Can "
                                 + "the agent do this at all?",
            "High, with most scenarios passing at least once.",
            "Low: the agent cannot do these even with k tries.",
            "It shows what the agent can reach; with one repeat it is the same number as pass^k. A trial that failed "
            + "for the infrastructure is left out, not counted as a fail.",
            "Higher is better."),
        ChartTopic.PassHat => new("pass^k: the share of scenarios that passed every time in their k trials. Can a "
                                  + "customer rely on it?",
            "High, close to pass@k.",
            "Far below pass@k: the agent can do it, but not every time.",
            "A product that is sold must work every time, not once in k tries; the gap between the two lines is the "
            + "unreliability.",
            "Higher is better."),
        ChartTopic.Value => new("What one real answer cost: the cost of the conversations routed right and (for a "
                                + "meal) priced 80% or more with sources, divided by their number.",
            "Low, with most of the ring green.",
            "Red: money spent without an answer.",
            "Cheap is worth nothing without an answer; this is the cost that matters.",
            "Lower is better, as long as there are answers."),
        _ => throw new ArgumentOutOfRangeException(nameof(topic), topic, null),
    };
}
