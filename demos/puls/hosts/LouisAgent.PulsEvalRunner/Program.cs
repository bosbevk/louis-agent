using LouisAgent.Core.Hosting;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Ollama;
using LouisAgent.Core.OpenAi;
using LouisAgent.Core.Telemetry;
using LouisAgent.Evals.Runner;
using LouisAgent.PulsEvalRunner;

// Puls's evals: the framework's runner, with the checks and readers they can name.
return await EvalRunnerHost.RunAsync(args, PulsCatalogs.All,
    new HostServices(Telemetry: OtlpTelemetry.Instance,
        Providers: [AnthropicProvider.Instance, OllamaProvider.Instance, OpenAiCompatibleProvider.Instance]));
