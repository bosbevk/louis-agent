// A stand-in for the eval runner (F18 step 7.4). The API starts the runner as `dotnet {runner.dll} run {check} --repeat N`, `... run {check} --rescore {id}` or `... --calibrate {check} {grader} --labels -`;
// the check's id, always the second argument, picks what this does, so a test names the behaviour it wants by the check it starts:
//
//   args    prints every argument, one per line          echo    prints `repeat {N}` and `api {PULS_API}`        run-id  prints BENCHMARK_RUN
//   env     prints `env:{NAME}` for every variable it can see, and `api=` and `run=` with their values         slow    prints `start`, then sleeps 2 s and never reads stdin
//   late    prints `one`, sleeps 1 s, prints `two`       fail    writes `boom` to stderr and exits 3              stdin   prints each line of stdin as `in:{line}`
//   sleeper sleeps 4 s and never reads stdin (a runner slow to take the export it is handed)

if (args.Length < 2) return 64;

switch (args[1])
{
    case "args":
        foreach (string argument in args) Console.WriteLine(argument);
        return 0;
    case "echo":
        Console.WriteLine($"repeat {args[(Array.IndexOf(args, "--repeat") + 1)..][0]}");
        Console.WriteLine($"api {Environment.GetEnvironmentVariable("PULS_API")}");
        return 0;
    case "run-id":
        Console.WriteLine(Environment.GetEnvironmentVariable("BENCHMARK_RUN"));
        return 0;
    case "env":
        foreach (string name in Environment.GetEnvironmentVariables().Keys.Cast<string>().Order(StringComparer.Ordinal))
            Console.WriteLine($"env:{name}");
        Console.WriteLine($"api={Environment.GetEnvironmentVariable("PULS_API")}");
        Console.WriteLine($"run={Environment.GetEnvironmentVariable("BENCHMARK_RUN")}");
        return 0;
    case "slow":
        Console.WriteLine("start");
        Thread.Sleep(2000);
        return 0;
    case "sleeper":
        Thread.Sleep(4000);
        return 0;
    case "late":
        Console.WriteLine("one");
        Thread.Sleep(1000);
        Console.WriteLine("two");
        return 0;
    case "fail":
        Console.Error.WriteLine("boom");
        return 3;
    case "stdin":
        while (Console.In.ReadLine() is { } line) Console.WriteLine($"in:{line}");
        return 0;
    default:
        return 64;
}
