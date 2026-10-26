using Microsoft.JSInterop;

namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>
/// Who the reviewer is, and what they have graded this session (F18 step 6a.3). The name is asked once and kept in the browser's storage (every call is in try/catch: storage may be blocked,
/// and the page then works for this visit only). The grades given this session are remembered so the judge's chip shows again at once, before any reload (blind review hides it until then).
/// A scoped service, so the chips and the review panel see the same name and the same grades.
/// </summary>
public sealed class ReviewerState(IJSRuntime js)
{
    public const string StorageKey = "louis-reviewer";

    private readonly HashSet<(ConversationRef Conversation, string Judge)> _graded = [];
    private bool _loaded;

    public string? Name { get; private set; }

    /// <summary>Raised when the name or the session's grades change, so what depends on them draws again.</summary>
    public event Action? Changed;

    /// <summary>Reads the kept name, once.</summary>
    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            Name = ReviewerName.Clean(await js.InvokeAsync<string?>("localStorage.getItem", StorageKey));
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException)
        {
            // Storage blocked or unavailable: no remembered name, and the person types one for this visit.
        }
        if (Name is not null) Changed?.Invoke();
    }

    /// <summary>Takes a typed name; false when it is blank or too long. It is kept in the browser if the browser lets it.</summary>
    public async Task<bool> SetAsync(string typed)
    {
        if (ReviewerName.Clean(typed) is not { } name) return false;
        Name = name;
        _loaded = true;
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, name);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException)
        {
            // Not remembered, but the name works for this visit.
        }
        Changed?.Invoke();
        return true;
    }

    public bool Graded(ConversationRef conversation, string judge) => _graded.Contains((conversation, judge));

    public void Record(ConversationRef conversation, string judge)
    {
        _graded.Add((conversation, judge));
        Changed?.Invoke();
    }
}
