using LouisAgent.Core.Messages;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Evals.Grading.Models;

/// <summary>
/// A model grader (F18 step 6.1): an <see cref="IEvaluator"/> from Microsoft.Extensions.AI.Evaluation behind <see cref="ICheck"/>, for what code can't judge. It hands the
/// evaluator the trial's messages and its answer, and maps the evaluator's one yes-or-no metric: true is a pass, false a fail, and no value (or a rating of unknown or
/// inconclusive) is Unknown, never a guessed pass. With no judge configured the grade is Unknown too, and the run goes on. Each grade says what judged
/// (<see cref="GradeJudge"/>), and the judge's requests run in a <see cref="UsagePurpose.Grading"/> scope tagged like the trial's turns, so the ledger counts them on the conversation and its run. Not
/// required until its calibration passes (the plan's step 6).
/// </summary>
public sealed class EvaluatorCheck : ICheck
{
    private readonly IEvaluator _evaluator;
    private readonly Lazy<ConfiguredJudge?> _judge;
    private readonly string _name, _promptHash;
    private readonly int _version;
    private readonly Func<Trial, IEnumerable<EvaluationContext>>? _context;

    public EvaluatorCheck(IEvaluator evaluator, ConfiguredJudge? judge, string name, int version, string promptHash,
        IReadOnlySet<string>? expectedKeys = null,
        Func<Trial, IEnumerable<EvaluationContext>>? context = null)
        : this(evaluator, () => judge, name, version, promptHash, expectedKeys, context)
    {
    }

    /// <summary>The judge is built when a trial is first graded, not when the eval is listed, so listing reads no settings and opens no ledger.</summary>
    public EvaluatorCheck(IEvaluator evaluator, Func<ConfiguredJudge?> judge, string name, int version,
        string promptHash, IReadOnlySet<string>? expectedKeys = null,
        Func<Trial, IEnumerable<EvaluationContext>>? context = null)
    {
        (_evaluator, _judge, _name, _version, _promptHash, _context) = (evaluator, new Lazy<ConfiguredJudge?>(judge),
            name, version, promptHash, context);
        ExpectedKeys = expectedKeys ?? new HashSet<string>();
    }

    public string Name => _name;
    public int Version => _version;
    public CheckKind Kind => CheckKind.Model;
    public IReadOnlySet<string> ExpectedKeys { get; }
    public bool Required => false;

    public async ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } failed) return failed;
        if (_judge.Value is not { } judge)
            return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoJudge));

        var (messages, answer) = Conversation(trial);
        // Tagged as the turns are, {run}:{mode}:{repeat}, and placed on the conversation's last turn, so the API's join puts the judging on its conversation and a slice's cost counts it.
        var last = trial.Transcript.Turns.Count == 0 ? null : trial.Transcript.Turns[^1];
        using var scope = UsageScope.Begin(UsagePurpose.Grading, session: last?.Session, turn: last?.Turn,
            tags: new UsageTags(trial.Scenario.Id, $"{trial.RunId}:{trial.Mode.Name}:{trial.Repeat}", null));
        var result =
            await _evaluator.EvaluateAsync(messages, answer, judge.Chat, _context?.Invoke(trial), cancellationToken);
        return Read(result) with { Judge = new GradeJudge(judge.Model, _promptHash) };
    }

    /// <summary>The user's side of the conversation, the earlier answers between, and the last answer as the response being judged.</summary>
    private static (List<ChatMessage> Messages, ChatResponse Answer) Conversation(Trial trial)
    {
        var turns = trial.Transcript.Turns;
        var messages = new List<ChatMessage>();
        for (int i = 0; i < turns.Count; i++)
        {
            messages.Add(new ChatMessage(ChatRole.User, turns[i].Question));
            if (i < turns.Count - 1) messages.Add(new ChatMessage(ChatRole.Assistant, EngineEvents.AnswerOf(turns[i])));
        }

        string last = turns.Count == 0 ? "" : EngineEvents.AnswerOf(turns[^1]);
        return (messages, new ChatResponse(new ChatMessage(ChatRole.Assistant, last)));
    }

    private Grade Read(EvaluationResult result)
    {
        var metrics = result.Metrics.Values.OfType<BooleanMetric>().ToList();
        if (metrics.Count != 1)
            return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.JudgeNoVerdict));

        var metric = metrics[0];
        string? reason = string.IsNullOrWhiteSpace(metric.Reason) ? null : metric.Reason;
        bool unrated = metric.Interpretation?.Rating is EvaluationRating.Unknown or EvaluationRating.Inconclusive;
        return metric.Value switch
        {
            true when !unrated => Verdict.Pass(Name, Version,
                reason ?? MessageText.Format(CommonGradeMessage.JudgeSaidYes)),
            false when !unrated => Verdict.Fail(Name, Version,
                reason ?? MessageText.Format(CommonGradeMessage.JudgeSaidNo)),
            _ => Verdict.Unknown(Name, Version, reason ?? MessageText.Format(CommonGradeMessage.JudgeNoVerdict)),
        };
    }
}
