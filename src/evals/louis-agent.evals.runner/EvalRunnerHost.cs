using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Telemetry;
using LouisAgent.Evals;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Calibration;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Refusal;
using LouisAgent.Evals.Runner.Promptfoo;
using LouisAgent.Evals.Runner.Studio;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner;

/// <summary>
/// The command that lists and runs evals. You say which checks and readers your evals can name, and the command line
/// and EVALS= pick the evals: the catalogue checks them, and this runs the one you named.
/// </summary>
/// <remarks>
/// Composition Root: your program's entry point is the one place that wires the app, so this library never names a pack
/// or an identity. Without that it would have to reference someone's project, and a package can't.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0074-the-folders-say-what-ships.md">ADR 0074: framework and implementation are split</seealso>
public static class EvalRunnerHost
{
    /// <summary>
    /// Does what the command line asks and gives back the exit code. stdout carries only the protocol and the --list
    /// lines; refusals and logs go to stderr.
    /// </summary>
    public static async Task<int> RunAsync(string[] args, RunnerCatalogs catalogs)
    {
        // It loads no .env and isn't built through AgentHost, so it starts its traces first, from what it was given.
        OtlpTelemetry.Instance.Start();
        try
        {
            var request = CommandLine.Parse(args);

            // Every eval folder copied beside the runner is found, its checks built from the catalogues you gave;
            // EVALS= then chooses them by the identity each one proves.
            var files = EvalFolders.Find(Path.Combine(AppContext.BaseDirectory, "EvalFiles"))
                .Select(folder =>
                    FileEval.Load(folder.Folder, catalogs.CheckCatalog, catalogs.ReaderCatalog, folder.Source))
                .ToList();
            var chosen = EvalFolders.Choose(files, Environment.GetEnvironmentVariable("EVALS"));
            var catalog = new EvalCatalog(chosen, new StubEval());

            switch (request)
            {
                case ListRequest:
                    foreach (var listed in catalog.Listing())
                        Console.Out.WriteLine(JsonSerializer.Serialize(listed, EvalJson.SnakeCase));
                    return 0;

                // The catalogue the studio composes an eval from: every check type with its settings' schema, then the
                // readers.
                case ChecksRequest:
                    foreach (string line in ChecksListing.Lines(catalogs.CheckCatalog, catalogs.ReaderCatalog))
                        Console.Out.WriteLine(line);
                    return 0;

                // A folder the studio wrote, judged as a run would judge it: its problems, then whether it was ok.
                case CheckRequest check:
                    return await CheckCommand.RunAsync(check.Folder, catalogs, Console.Out, CancellationToken.None);

                // promptfoo in and out: a file to a folder, naming what was left out, and a folder to a file.
                case ImportPromptfooRequest import:
                    foreach (string left in PromptfooImport.Write(File.ReadAllText(import.File), import.Folder))
                        Console.Out.WriteLine(JsonSerializer.Serialize(new LeftOutLine(left), EvalJson.SnakeCase));
                    return 0;

                case ExportPromptfooRequest export:
                    Console.Out.Write(PromptfooExport.Yaml(export.Folder));
                    return 0;

                // The two commands that check a model grader against a person. Neither opens a session; --calibrate asks
                // the judge, so it refuses in CI.
                case CalibrateRequest calibrate:
                    return await CalibrateCommand.RunAsync(calibrate, catalog.Get(calibrate.Eval),
                        Environment.GetEnvironmentVariable, Console.In, Console.Out, CancellationToken.None);

                case LabelSheetRequest sheet:
                    return LabelSheetCommand.Run(sheet, catalog.Get(sheet.Eval),
                        Console.In.ReadToEnd()
                            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                        Console.Out);

                // A run of the catalogue's eval, or of a studio folder's on a few of its samples (a try, F49.5).
                case RunRequest run:
                    var eval = RunTarget.Of(run, catalog, catalogs);
                    // A re-score grades the stored run's lines, which the API pipes to stdin. It opens no session, so it
                    // has no client to call.
                    if (run.RescoreOf is { } source)
                    {
                        var rescorer = new EvalRunner(new NoSessionClient(),
                            new ProtocolWriter(Console.Out, TimeProvider.System), TimeProvider.System,
                            AgentLogging.For<EvalRunner>());
                        var stored = Console.In.ReadToEnd().Split('\n',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        await rescorer.RescoreAsync(eval,
                            BenchmarkRun.Of(Environment.GetEnvironmentVariable("BENCHMARK_RUN"), TimeProvider.System),
                            source,
                            stored,
                            CancellationToken.None);
                        return 0;
                    }

                    // The stub answers from canned turns; every other eval talks to the API at AGENT_API (or PULS_API, its
                    // old name), with AGENT_API_KEY when it is set.
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
                        BenchmarkRun.Of(Environment.GetEnvironmentVariable("BENCHMARK_RUN"), TimeProvider.System),
                        run.Repeat,
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
    }
}
