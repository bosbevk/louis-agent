using LouisAgent.Web.Workspace;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Workspace;

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

    protected override void OnParametersSet()
    {
        LineCount = File.Content.Length == 0 ? 0 : File.Content.TrimEnd('\n').Split('\n').Length;
        _gutter = string.Join('\n', Enumerable.Range(1, Math.Max(LineCount, 1)));
        _language = CodeLanguage.For(File.Name);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (ReferenceEquals(_renderedFile, File)) return;
        _renderedFile = File;
        await JS.InvokeVoidAsync("louisChat.renderCode", _code, File.Content, _language);
    }
}
