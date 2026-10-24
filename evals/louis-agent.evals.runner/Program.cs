using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Evals;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.Evals.Runner.Stub;
using LouisAgent.Evals.Running;

// The runner's bootstrap (F18 step 4.1): the command line and EVALS= choose the evals, the catalogue checks them, and the runner
// runs the one named. stdout is the protocol and the --list lines, and nothing else: refusals and logs go to stderr.
try
{
    var request = CommandLine.Parse(args);
    var sets = EvalSetSelection.Parse(Environment.GetEnvironmentVariable("EVALS")).Select(EvalSets.Create).ToList();
    var catalog = new EvalCatalog(sets, new StubEval());

    switch (request)
    {
        case ListRequest:
            foreach (var listed in catalog.Listing())
                Console.Out.WriteLine(JsonSerializer.Serialize(listed, EvalJson.SnakeCase));
            return 0;

        case RunRequest run:
            var eval = catalog.Get(run.Name);
            // Only the stub has a session client now; the HTTP client comes with the first real eval (step 5).
            ISessionClient session = eval is StubEval
                ? new StubSessionClient()
                : throw new RunnerException(MessageText.Format(RunnerMessage.NoSessionClient, new { Name = eval.Name }));
            var runner = new EvalRunner(session, new ProtocolWriter(Console.Out, TimeProvider.System), TimeProvider.System,
                AgentLogging.For<EvalRunner>());
            // The run's name is the ledger's tag: BENCHMARK_RUN, as the Python checks read it.
            await runner.RunAsync(eval, new EvalRun(BenchmarkRun.Of(Environment.GetEnvironmentVariable("BENCHMARK_RUN")), run.Repeat),
                CancellationToken.None);
            return 0;

        default:
            return 1;
    }
}
catch (RunnerException error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
