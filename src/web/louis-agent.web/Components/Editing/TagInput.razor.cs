using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LouisAgent.Web.Components.Editing;

/// <summary>
/// A field of short words, such as a job's stack or a skill group's skills. The words sit in the field as chips, and
/// you type the next one after them: Enter or a comma adds it, a chip's cross or Backspace in an empty box removes
/// one.
/// </summary>
/// <remarks>
/// It never changes the list it was given: each add or remove hands back a new list through
/// <see cref="TagsChanged"/>, as <see cref="ListEdits"/> does for any list. A word is trimmed, an empty one is
/// dropped and one already there (in any case) isn't added twice, so the field can't hold a blank or a repeat.
/// </remarks>
public partial class TagInput
{
    /// <summary>What has been typed and not yet added.</summary>
    private string _draft = "";

    /// <summary>The tags as they stand.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>Hands back the tags after one is added or removed.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<string>> TagsChanged { get; set; }

    /// <summary>The field's name, for a screen reader: the box has no visible label of its own.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>What the empty box says, such as "Add a skill".</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Keeps what is typed; a comma adds everything before it, so a pasted list becomes tags.</summary>
    private Task TypedAsync(ChangeEventArgs typed)
    {
        _draft = typed.Value?.ToString() ?? "";
        if (!_draft.Contains(',')) return Task.CompletedTask;

        // The words before the last comma are finished; what follows it is still being typed.
        int last = _draft.LastIndexOf(',');
        string finished = _draft[..last];
        _draft = _draft[(last + 1)..].TrimStart();
        return AddAsync(finished.Split(','));
    }

    /// <summary>Enter adds what is typed; Backspace in an empty box takes the last tag off.</summary>
    /// <remarks>Guard clauses, one per key (ADR 0051).</remarks>
    private Task KeyAsync(KeyboardEventArgs key)
    {
        if (key.Key == "Enter") return KeepAsync();
        if (key.Key == "Backspace" && _draft.Length == 0 && Tags.Count > 0) return RemoveAsync(Tags[^1]);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds what is typed and empties the box: on Enter, and on leaving the box, so a word isn't lost.
    /// </summary>
    private Task KeepAsync()
    {
        string typed = _draft;
        _draft = "";
        return AddAsync([typed]);
    }

    /// <summary>Adds the words that are new, trimmed; nothing is handed back when none is.</summary>
    private Task AddAsync(IEnumerable<string> words)
    {
        var tags = Tags.ToList();
        foreach (string word in words.Select(word => word.Trim()).Where(word => word.Length > 0))
        {
            if (!tags.Contains(word, StringComparer.OrdinalIgnoreCase)) tags.Add(word);
        }

        return tags.Count == Tags.Count ? Task.CompletedTask : TagsChanged.InvokeAsync(tags);
    }

    /// <summary>Takes the tag off.</summary>
    private Task RemoveAsync(string tag) => TagsChanged.InvokeAsync([.. Tags.Where(kept => kept != tag)]);
}
