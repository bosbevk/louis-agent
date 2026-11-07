using LouisAgent.Core.Hosting;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Ollama;
using LouisAgent.Core.OpenAi;
using LouisAgent.Core.Telemetry;
using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Catalog;

// The coding agent has no evals of its own yet, so this is the stub and the harness's general checks.
return await EvalRunnerHost.RunAsync(args, RunnerCatalogs.GeneralOnly,
    new HostServices(Telemetry: OtlpTelemetry.Instance,
        Providers: [AnthropicProvider.Instance, OllamaProvider.Instance, OpenAiCompatibleProvider.Instance]));
