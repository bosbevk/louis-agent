namespace LouisAgent.Api.Evals.Store;

/// <summary>
/// Runs a setup (the store's indexes) until it has worked once. Callers at the same time share one try, and a failed
/// try is forgotten, so the next call has another go.
/// </summary>
/// <remarks>
/// Lazy Initialization that keeps only a success. A <see cref="Lazy{T}"/> of a task would keep the failure for ever: a
/// store asked while its database was still starting stayed down until the API restarted.
/// </remarks>
public sealed class OnceItWorks(Func<Task> setup)
{
    private readonly object _gate = new();
    private Task? _attempt;

    /// <summary>Starts the setup, or hands you the try that's running or already worked.</summary>
    public Task RunAsync()
    {
        lock (_gate)
        {
            // A finished attempt that failed is dropped; one still running, or one that worked, is shared.
            if (_attempt is null || _attempt.IsFaulted || _attempt.IsCanceled) _attempt = setup();
            return _attempt;
        }
    }
}
