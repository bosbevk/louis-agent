using LouisAgent.Core.Logging;

namespace LouisAgent.Packs.WhereToBuy.Tests;

/// <summary>
/// Stderr, captured while a test runs (F18 step 4f.2): the log lines the map lookup writes. Starts the logging in its default mode, where a
/// value shows as a keyed hash and a length, and puts stderr back on dispose. Console.Error is process-wide, so a test that uses this is
/// <c>[NonParallelizable]</c>.
/// </summary>
internal sealed class CapturedStderr : IDisposable
{
    private readonly TextWriter _original = Console.Error;
    private readonly StringWriter _written = new();

    public CapturedStderr()
    {
        Console.SetError(_written);
        AgentLogging.Configure(null);
    }

    public string Text => _written.ToString();

    public void Dispose() => Console.SetError(_original);
}
