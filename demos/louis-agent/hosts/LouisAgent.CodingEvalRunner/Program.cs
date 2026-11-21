using LouisAgent.Core.Hosting;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Ollama;
using LouisAgent.Core.OpenAi;
using LouisAgent.Core.Telemetry;
using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Catalog;

// The coding demo's evals (F106.3.2: the triage team's roles, one eval each) name only the harness's general checks.
return await EvalRunnerHost.RunAsync(args, RunnerCatalogs.GeneralOnly,
    new HostServices(Telemetry: OtlpTelemetry.Instance,
        Providers: [AnthropicProvider.Instance, OllamaProvider.Instance, OpenAiCompatibleProvider.Instance]));
