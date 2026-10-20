namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// Setup that runs until it works once (the store's indexes): callers at the same time share one attempt, a success is
/// kept, and a failure is not, so the next call tries again. A <see cref="Lazy{T}"/> of a task would keep the failure
/// for ever: a store asked while its database was still starting stayed down until the API restarted.
/// </summary>
public sealed class OnceItWorks(Func<Task> setup)
{
    private readonly object _gate = new();
    private Task? _attempt;

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
