using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Choices;

namespace LouisAgent.Evals.Grading.Models;

/// <summary>
/// Which model judges (F18 step 6.1): <c>EVAL_JUDGE_PROVIDER</c> and <c>EVAL_JUDGE_MODEL</c>, with <c>EVAL_JUDGE_ENDPOINT</c> for a local server, read once here with
/// <see cref="SettingChoice"/> (ADR 0024). Null when no model is set: there is no judge, which a run survives. Without a provider, a Claude model is Anthropic's and any
/// other is the local one, so a judge nobody asked to pay for is free.
/// </summary>
public sealed record JudgeSettings(JudgeProvider Provider, string Model, string? Endpoint, string? ApiKey)
{
    /// <summary>Never the key: a record prints all its members, and a log line with a key in it is a leaked key.</summary>
    public override string ToString() => $"{Provider}:{Model} (endpoint={Endpoint ?? "default"}, apiKey={(string.IsNullOrEmpty(ApiKey) ? "unset" : "set")})";

    private static readonly Dictionary<string, JudgeProvider> Aliases = new() { ["openai-compatible"] = JudgeProvider.OpenAiCompatible };

    public static JudgeSettings? FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();

        if (Get("EVAL_JUDGE_MODEL") is not { } model) return null;
        string? named = Get("EVAL_JUDGE_PROVIDER");
        if (!SettingChoice.TryParse(named, out JudgeProvider? choice, Aliases))
            throw new InvalidDataException(MessageText.Format(EvalErrorMessage.UnknownJudgeProvider,
                new { Value = named, Supported = string.Join(", ", SettingChoice.Names<JudgeProvider>()) }));

        var provider = choice ?? (model.StartsWith("claude", StringComparison.OrdinalIgnoreCase) ? JudgeProvider.Anthropic : JudgeProvider.Ollama);
        string? key = Get("EVAL_JUDGE_API_KEY") ?? (provider == JudgeProvider.Anthropic ? Get("ANTHROPIC_API_KEY") : null);
        return new JudgeSettings(provider, model, Get("EVAL_JUDGE_ENDPOINT"), key);
    }
}
