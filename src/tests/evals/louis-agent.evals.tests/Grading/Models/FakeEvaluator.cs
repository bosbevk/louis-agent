using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Evals.Tests.Grading.Models;

/// <summary>An IEvaluator that returns the metrics it was given (or throws the error it was given) and keeps what it was asked.</summary>
internal sealed class FakeEvaluator : IEvaluator
{
    private readonly EvaluationMetric[] _metrics;
    private readonly Exception? _error;

    public FakeEvaluator(params EvaluationMetric[] metrics) => _metrics = metrics;

    public FakeEvaluator(Exception error)
    {
        _metrics = [];
        _error = error;
    }

    public int Calls { get; private set; }
    public List<ChatMessage> Messages { get; private set; } = [];
    public ChatResponse? Response { get; private set; }
    public ChatConfiguration? Chat { get; private set; }
    public List<EvaluationContext> Context { get; private set; } = [];

    /// <summary>What a real evaluator does with the judge: it asks the model.</summary>
    public Func<ChatConfiguration, Task>? OnEvaluate { get; init; }

    public IReadOnlyCollection<string> EvaluationMetricNames => _metrics.Select(m => m.Name).ToArray();

    public async ValueTask<EvaluationResult> EvaluateAsync(IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse, ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null, CancellationToken cancellationToken = default)
    {
        Calls++;
        Messages = messages.ToList();
        Response = modelResponse;
        Chat = chatConfiguration;
        Context = additionalContext?.ToList() ?? [];
        if (_error is not null) throw _error;
        if (OnEvaluate is not null) await OnEvaluate(chatConfiguration!);
        return new EvaluationResult(_metrics);
    }
}
