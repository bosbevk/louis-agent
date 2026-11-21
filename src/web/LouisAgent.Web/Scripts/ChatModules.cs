using Microsoft.JSInterop;

namespace LouisAgent.Web.Scripts;

/// <summary>
/// The chat frame's three modules on one runtime (CL31): the shell's own (the page-wide behaviour, attached while
/// the frame is drawn), the dialogs' focus, and what the browser remembers for the person, which the frame's
/// preferences and languages share. Built when the frame starts, let go together when it goes.
/// </summary>
/// <remarks>
/// A small Facade over three <see cref="JsModule"/>s, so the frame holds one thing and its disposal takes the
/// listeners down before the modules go; without it the frame carried three fields and the order itself.
/// </remarks>
public sealed class ChatModules(IJSRuntime js) : IAsyncDisposable
{
    private JsModule? _shell, _dialogs, _preferences;

    /// <summary>The shell's own module: the sidebar, the work panel, the cards, the costs, focus, copy, the time zone.</summary>
    public JsModule Shell => _shell ??= new JsModule(js, FrameworkScripts.Shell);

    /// <summary>Focus for a dialog or a field Blazor just drew.</summary>
    public JsModule Dialogs => _dialogs ??= new JsModule(js, FrameworkScripts.Dialogs);

    /// <summary>What this browser remembers for the person.</summary>
    public JsModule Preferences => _preferences ??= new JsModule(js, FrameworkScripts.Preferences);

    /// <summary>Takes the page's listeners down if the shell's module was ever loaded, then lets every module go.</summary>
    public async ValueTask DisposeAsync()
    {
        // A closed browser tab has no JS runtime to tell.
        try
        {
            if (_shell is not null) await _shell.InvokeVoidAsync("detach");
        }
        catch (JSDisconnectedException)
        {
        }

        foreach (var module in new[] { _shell, _dialogs, _preferences })
            if (module is not null)
                await module.DisposeAsync();
    }
}
