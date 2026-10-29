using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Choices;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Admin.Replies;

/// <summary>
/// Builds the rows of a stored run's read from what the store kept: its modes, conversations and their grades, the
/// people's grades, its turns, its requests and a turn's events.
/// </summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, for the reason
/// <c>AdminReplies</c> gives: the records live in the contract, which can't see the store's documents. The enums are
/// spelt with <c>Wire()</c>, as the protocol and the store spell them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class StoredRunReplies
{
    /// <summary>A mode's row carries its runner rates, null on a mode from before them.</summary>
    extension(ModeRow)
    {
        /// <summary>The row for one repeat of a mode.</summary>
        public static ModeRow Of(Mode m) => new(m.Name, m.Repeat, m.Toolsets, m.Role, m.Llm, m.Rates?.PassAtK,
            m.Rates?.PassHatK, m.Rates?.K, m.Rates?.Infrastructure, m.Rates?.Retried);
    }

    /// <summary>A conversation's row carries its grades and the people's latest grades of it.</summary>
    extension(ConversationRow)
    {
        /// <summary>The row for one conversation, with the people's grades given.</summary>
        public static ConversationRow Of(Conversation c, IReadOnlyList<HumanGrade> human) => new(
            c.Key.Mode,
            c.Key.Repeat,
            c.Key.Task,
            c.Order,
            c.Role,
            c.Errors,
            c.Seconds,
            c.Grades?.Select(g => new GradeRow(g.Grader, g.Version, g.Score, g.Outcome.Wire(), g.Reason,
                g.JudgeModel, g.JudgePromptHash)).ToList(),
            c.Outcome?.Wire(),
            [.. human.Select(g => HumanGradeRow.Of(g, latest: true))],
            c.Metrics is { } m
                ? new MetricsRow(m.AgentCalls, m.SpecialistCalls, m.Hosts, m.Turns, m.TokensIn, m.TokensOut,
                    m.Seconds, m.Cost)
                : null);
    }

    /// <summary>A person's grade names the judge it grades both ways: with its human suffix, and plain.</summary>
    extension(HumanGradeRow)
    {
        /// <summary>The row for one person's grade, saying whether it is that person's latest.</summary>
        public static HumanGradeRow Of(HumanGrade g, bool latest) => new(
            g.Conversation.Mode,
            g.Conversation.Repeat,
            g.Conversation.Task,
            g.Grader + HumanReview.Suffix,
            g.Grader,
            g.Verdict.Wire(),
            g.Note,
            g.Reviewer,
            g.At,
            latest);
    }

    /// <summary>A turn's row carries what its tools did, read from its stored events.</summary>
    extension(TurnRow)
    {
        /// <summary>The row for one turn and its tools.</summary>
        public static TurnRow Of(Turn t, EvalTools.Summary tools) => new(
            new TurnToolsRow(tools.Calls, tools.Failed,
                [.. tools.Reasons.Select(r => new ToolReasonRow(r.Tool, r.Reason, r.Count))]),
            t.Key.Mode,
            t.Key.Repeat,
            t.Key.Task,
            t.Key.Turn,
            t.Session,
            t.Question,
            t.Answer,
            t.Role,
            t.Continued,
            t.AgentCalls,
            t.SpecialistCalls,
            t.Hosts,
            t.Seconds,
            t.Error);
    }

    /// <summary>A request's row names its turn when the ledger could tie it to one.</summary>
    extension(RequestRow)
    {
        /// <summary>The row for one model request from the ledger.</summary>
        public static RequestRow Of(Request r) => new(
            r.Turn is null ? null : new TurnKeyRow(r.Turn.Mode, r.Turn.Repeat, r.Turn.Task, r.Turn.Turn),
            r.At,
            r.Purpose,
            r.Session,
            r.Parent,
            r.Model,
            r.InputTokens,
            r.OutputTokens,
            r.CacheReadTokens,
            r.CacheWriteTokens,
            r.Cost,
            r.DurationMs);
    }

    /// <summary>A judge's agreement with the people, as the counts the review keeps.</summary>
    extension(AgreementRow)
    {
        /// <summary>The row for one judge, from the people's decided grades of the run.</summary>
        public static AgreementRow Of(string grader, IReadOnlyList<Conversation> conversations,
            IReadOnlyList<HumanGrade> decided)
        {
            // Count how the judge's grades line up with the people's, then split them by what the people said.
            var t = ReviewAgreement.Of(conversations, decided, grader);

            return new AgreementRow(grader,
                new CalibrationClassRow(t.PersonPassJudgePass, t.PersonPassJudgeFail, t.PersonPassJudgeUnknown),
                new CalibrationClassRow(t.PersonFailJudgePass, t.PersonFailJudgeFail, t.PersonFailJudgeUnknown));
        }
    }

    /// <summary>A turn's event, as it was sent.</summary>
    extension(TurnEventRow)
    {
        /// <summary>The row for one stored stream event.</summary>
        public static TurnEventRow Of(StreamEvent e) => new(e.Type, e.Data, e.At);
    }
}
