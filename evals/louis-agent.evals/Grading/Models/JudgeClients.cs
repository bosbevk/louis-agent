using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Usage.Sinks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Evals.Grading.Models;

/// <summary>
/// Builds the judge's client (F18 step 6.1) through the engine's <see cref="ILlmClientFactory"/>, as every other model client is built, and records its requests in the
/// usage ledger like any other (ADR 0007): the grader's scope gives them the <c>grading</c> purpose and the run's tag. With no ledger the judge still asks.
/// </summary>
public static class JudgeClients
{
    /// <summary>The judge the environment names, or null when it names none; the ledger is opened (by <paramref name="ledger"/>) only for a judge there is.</summary>
    public static ConfiguredJudge? FromEnvironment(Func<string, string?> getVariable, ILlmClientFactory factory,
        Func<IUsageSink?> ledger) =>
        JudgeSettings.FromEnvironment(getVariable) is { } settings ? Create(settings, factory, ledger()) : null;

    /// <summary>The judge this process's environment names, with the real client factory and the ledger the process writes (<c>LOG_DIRECTORY</c>); null when it names none.</summary>
    public static ConfiguredJudge? FromEnvironment() =>
        FromEnvironment(Environment.GetEnvironmentVariable, new LlmClientFactory(),
            () => UsageLedger.Open(AgentOptions.FromEnvironment()));

    public static ConfiguredJudge Create(JudgeSettings settings, ILlmClientFactory factory, IUsageSink? sink)
    {
        var options = new LlmOptions
        {
            Provider = settings.Provider switch
            {
                JudgeProvider.Anthropic => LlmOptions.Anthropic,
                JudgeProvider.OpenAiCompatible => LlmOptions.OpenAiCompatible,
                _ => LlmOptions.Ollama,
            },
            Model = settings.Model,
            Endpoint = settings.Endpoint,
            ApiKey = settings.ApiKey,
        };
        IChatClient client = factory.Create(options);
        if (sink is not null)
            client = new UsageRecordingChatClient(client, sink, settings.Model, factory.CreateUsageMapper(options));
        return new ConfiguredJudge(new ChatConfiguration(client), settings.Model);
    }
}
