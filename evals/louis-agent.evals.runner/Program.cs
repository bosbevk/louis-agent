using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Evals;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Calibration;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.Evals.Runner.Refusal;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Runner.Evals.Stub;

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

        // F18 step 6.3: the two commands that check a model grader against a person. Neither opens a session; --calibrate asks the judge, so it refuses in CI.
        case CalibrateRequest calibrate:
            return await CalibrateCommand.RunAsync(calibrate, catalog.Get(calibrate.Eval), Environment.GetEnvironmentVariable, Console.Out, CancellationToken.None);

        case LabelSheetRequest sheet:
            return LabelSheetCommand.Run(sheet, catalog.Get(sheet.Eval), Console.In.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                Console.Out);

        case RunRequest run:
            var eval = catalog.Get(run.Name);
            // A re-score (F18 step 5b) grades the stored run's lines, which the API pipes to stdin, and opens no session: it has no client to call.
            if (run.RescoreOf is { } source)
            {
                var rescorer = new EvalRunner(new NoSessionClient(), new ProtocolWriter(Console.Out, TimeProvider.System), TimeProvider.System,
                    AgentLogging.For<EvalRunner>());
                string[] stored = Console.In.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                await rescorer.RescoreAsync(eval, BenchmarkRun.Of(Environment.GetEnvironmentVariable("BENCHMARK_RUN"), TimeProvider.System), source, stored,
                    CancellationToken.None);
                return 0;
            }
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
                run.Web, run.Of, run.Model, run.Split);
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
