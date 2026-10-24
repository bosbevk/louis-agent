using System.Globalization;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Runner.Messages;

namespace LouisAgent.Evals.Runner.Arguments;

/// <summary>What the command line asks for.</summary>
public abstract record CommandLineRequest;

/// <summary><c>--list</c>: the catalogue, one JSON object per line.</summary>
public sealed record ListRequest : CommandLineRequest;

/// <summary><c>run {name} --repeat N</c>: one eval, each mode repeated N times.</summary>
public sealed record RunRequest(string Name, int Repeat) : CommandLineRequest;

/// <summary>The command line: <c>--list</c>, or <c>run {name} [--repeat N]</c>. A refusal is a <see cref="RunnerException"/>.</summary>
public static class CommandLine
{
    /// <summary>
    /// Repeats one run may ask for, clamped to 1..MaxRepeat as the API's BenchmarkRunner clamps them. The API can't reference the
    /// runner, so this constant is its own: the two must match (src/louis-agent.api/Benchmarks/Runs/BenchmarkRunner.cs).
    /// </summary>
    public const int MaxRepeat = 5;

    public static CommandLineRequest Parse(IReadOnlyList<string> args)
    {
        if (args is ["--list"]) return new ListRequest();
        if (args is ["run", var name, ..]) return new RunRequest(name, ParseRepeat(args.Skip(2).ToList()));
        if (args.Count == 0 || args[0] == "run") throw Refuse(RunnerMessage.Usage, null);
        throw Refuse(RunnerMessage.UnknownArgument, new { Argument = args[0] });
    }

    private static int ParseRepeat(IReadOnlyList<string> rest)
    {
        int repeat = 1;
        for (int i = 0; i < rest.Count; i++)
        {
            if (rest[i] != "--repeat" || i + 1 >= rest.Count) throw Refuse(RunnerMessage.UnknownArgument, new { Argument = rest[i] });
            string value = rest[++i];
            if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int n))
                throw Refuse(RunnerMessage.BadRepeat, new { Value = value });
            repeat = Math.Clamp(n, 1, MaxRepeat);
        }
        return repeat;
    }

    private static RunnerException Refuse(RunnerMessage message, object? values) => new(MessageText.Format(message, values));
}
