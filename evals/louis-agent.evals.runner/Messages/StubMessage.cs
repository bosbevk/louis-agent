using System.ComponentModel;

namespace LouisAgent.Evals.Runner.Messages;

/// <summary>
/// The stub's canned texts: the questions it asks, what the model "thinks" and answers (ADR 0009: user and model text are message
/// enum members, as the Python stub's are its own strings). The same words as benchmarks/stub/run_check.py.
/// </summary>
public enum StubMessage
{
    [Description("Maak vir my bobotie vir 4 mense.")]
    BobotieFirst,

    [Description("En as ek dit vir 6 wil maak?")]
    BobotieFollowUp,

    [Description("Hoe maak ek cottage pie?")]
    CottagePie,

    [Description("Wat vra hulle? ")] ThinkingFirst,

    [Description("'n Resep.")] ThinkingSecond,

    [Description("Die kok se resep.")] CookReply,

    [Description("Hier is {Task}, ")] AnswerStart,

    [Description("stap vir stap.")] AnswerEnd,
}
