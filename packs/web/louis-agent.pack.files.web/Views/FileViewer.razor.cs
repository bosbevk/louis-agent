using LouisAgent.Packs.Files.Web.Reading;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Files.Web.Views;

/// <summary>One open file with line numbers and highlighting, and the buttons to review or ask about it.</summary>
public partial class FileViewer : IAsyncDisposable
{
    private JsModule? _code;

    /// <summary>The turn's module (CL31): the one highlighter for a turn's blocks and for a whole file.</summary>
    private JsModule Code => _code ??= new JsModule(JS, FrameworkScripts.TurnView);

    [Parameter, EditorRequired] public WorkspaceFile File { get; set; } = default!;
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback OnReview { get; set; }
    [Parameter] public EventCallback OnAsk { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private ElementReference _element;
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

    /// <summary>Hands a newly opened file to the turn's module to highlight; the same file isn't done twice.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (ReferenceEquals(_renderedFile, File)) return;
        _renderedFile = File;
        await Code.InvokeVoidAsync("renderCode", _element, File.Content, _language);
    }

    /// <summary>Lets the module go, if it was ever loaded.</summary>
    public ValueTask DisposeAsync() => _code?.DisposeAsync() ?? ValueTask.CompletedTask;
}
