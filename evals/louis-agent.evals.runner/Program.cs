using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Evals;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Calibration;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Refusal;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Runner.Evals.Stub;

// The runner starts here: the command line and EVALS= pick the evals, the catalogue checks them, and the runner runs
// the one you named. stdout carries only the protocol and the --list lines; refusals and logs go to stderr.
try
{
    var request = CommandLine.Parse(args);

    // EVALS= chooses the C# sets by name and the evals in files by the identity they name; every eval folder copied
    // beside the runner is found, its checks built from the catalogue the runner gathers.
    var evals = Environment.GetEnvironmentVariable("EVALS");
    var sets = EvalSetSelection.Parse(evals).Select(EvalSets.Create).ToList();
    var checks = new CheckCatalog(GeneralChecks.All);
    var files = EvalFolders.Find(Path.Combine(AppContext.BaseDirectory, "EvalFiles"))
        .Select(folder => FileEval.Load(folder.Folder, checks, folder.Source))
        .Where(file => EvalFolders.Chosen(evals, file.Identity));
    var catalog = new EvalCatalog(sets, new StubEval(), files);

    switch (request)
    {
        case ListRequest:
            foreach (var listed in catalog.Listing())
                Console.Out.WriteLine(JsonSerializer.Serialize(listed, EvalJson.SnakeCase));
            return 0;

        // The two commands that check a model grader against a person. Neither opens a session; --calibrate asks the
        // judge, so it refuses in CI.
        case CalibrateRequest calibrate:
            return await CalibrateCommand.RunAsync(calibrate, catalog.Get(calibrate.Eval),
                Environment.GetEnvironmentVariable, Console.In, Console.Out, CancellationToken.None);

        case LabelSheetRequest sheet:
            return LabelSheetCommand.Run(sheet, catalog.Get(sheet.Eval),
                Console.In.ReadToEnd()
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                Console.Out);

        case RunRequest run:
            var eval = catalog.Get(run.Name);
            // A re-score grades the stored run's lines, which the API pipes to stdin. It opens no session, so it has
            // no client to call.
            if (run.RescoreOf is { } source)
            {
                var rescorer = new EvalRunner(new NoSessionClient(),
                    new ProtocolWriter(Console.Out, TimeProvider.System), TimeProvider.System,
                    AgentLogging.For<EvalRunner>());
                var stored = Console.In.ReadToEnd().Split('\n',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                await rescorer.RescoreAsync(eval,
                    BenchmarkRun.Of(Environment.GetEnvironmentVariable("BENCHMARK_RUN"), TimeProvider.System), source,
                    stored,
                    CancellationToken.None);
                return 0;
            }

            // The stub answers from canned turns; every other eval talks to the API at AGENT_API (or PULS_API, its old
            // name), with AGENT_API_KEY when it is set.
            ISessionClient session = eval is StubEval
                ? new StubSessionClient()
                : new HttpSessionClient(HttpSessionClient.CreateHttp(
                    HttpSessionClient.ApiFrom(Environment.GetEnvironmentVariable),
                    Environment.GetEnvironmentVariable("AGENT_API_KEY")));
            var runner = new EvalRunner(session, new ProtocolWriter(Console.Out, TimeProvider.System),
                TimeProvider.System,
                AgentLogging.For<EvalRunner>());
            // The run's name is the ledger's tag: BENCHMARK_RUN, as the Python checks read it. --web, --of and
            // --replay pass on as asked; a run with none of them leaves the web to the instance's WEB_REPLAY.
            var recorded = new EvalRun(
                BenchmarkRun.Of(Environment.GetEnvironmentVariable("BENCHMARK_RUN"), TimeProvider.System), run.Repeat,
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
