using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Evals;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.Evals.Runner.Refusal;
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
            // The stub answers from canned turns; every other eval talks to the API (F18 step 5.1): PULS_API, and AGENT_API_KEY when set.
            ISessionClient session = eval is StubEval
                ? new StubSessionClient()
                : new HttpSessionClient(HttpSessionClient.CreateHttp(Environment.GetEnvironmentVariable("PULS_API"),
                    Environment.GetEnvironmentVariable("AGENT_API_KEY")));
            var runner = new EvalRunner(session, new ProtocolWriter(Console.Out, TimeProvider.System), TimeProvider.System,
                AgentLogging.For<EvalRunner>());
            // The run's name is the ledger's tag: BENCHMARK_RUN, as the Python checks read it.
            // --web, --of and --replay pass on as the run asked; a run with neither leaves the web to the instance's WEB_REPLAY (the API decides).
            var recorded = new EvalRun(BenchmarkRun.Of(Environment.GetEnvironmentVariable("BENCHMARK_RUN"), TimeProvider.System), run.Repeat,
                run.Web, run.Of, run.Model);
            await runner.RunAsync(eval, recorded, CancellationToken.None);
            return 0;

        default:
            return 1;
    }
}
catch (Exception error) when (RefusalReport.IsRefusal(error))
{
    return RefusalReport.Report(error, Console.Error);
}
