using LouisAgent.CodeReview.Workspace;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.CodeReview.Components.Workspace;

/// <summary>One open file with line numbers and highlighting, and the buttons to review or ask about it.</summary>
public partial class FileViewer
{
    [Parameter, EditorRequired] public WorkspaceFile File { get; set; } = default!;
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback OnReview { get; set; }
    [Parameter] public EventCallback OnAsk { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private ElementReference _code;
    private WorkspaceFile? _renderedFile;
    private string _gutter = "";
    private string? _language;
    private int LineCount { get; set; }

    /// <summary>Counts the lines for the gutter, and works out the language from the file's name.</summary>
    protected override void OnParametersSet()
    {
        LineCount = File.Content.Length == 0 ? 0 : File.Content.TrimEnd('\n').Split('\n').Length;
        _gutter = string.Join('\n', Enumerable.Range(1, Math.Max(LineCount, 1)));
        _language = CodeLanguage.For(File.Name);
    }

    /// <summary>Hands a newly opened file to the page's script to highlight; the same file isn't done twice.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (ReferenceEquals(_renderedFile, File)) return;
        _renderedFile = File;
        await JS.InvokeVoidAsync("louisChat.renderCode", _code, File.Content, _language);
    }
}
