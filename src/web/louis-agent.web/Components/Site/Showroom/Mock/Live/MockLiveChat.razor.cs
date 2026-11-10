using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Site.Showroom.Mock.Live;

/// <summary>
/// A business's agent, live, in the corner of its mocked website (F127.2, Louis: "Try it yourself"): the visitor asks,
/// the agent answers as the business from the website the visitor sees there, and a place it shows moves the website
/// as a recorded one does.
/// </summary>
/// <remarks>
/// Facade, in Razor (Gang of Four), as <c>ChatWidget</c> is: the chat's list, message box and notice, and the runner
/// that gives a turn its life, are the framework's own, behind one tag with the identity as its one setting, so
/// nothing of a chat is written twice. What it reads is the mock's page (<see cref="SiteMock.ReadPageAsync"/>), never
/// the real one around it, so the agent sees the business's website and not the site the demo sits on. A place it
/// shows goes out through <see cref="ToolCalled"/>, the hook the recording has, so the demo moves its website one way
/// for both. Its chats are kept under a key of their own, apart from the guide's.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://refactoring.guru/design-patterns/facade">Facade (Refactoring.Guru)</seealso>
public partial class MockLiveChat : IDisposable
{
    /// <summary>Where a try's chats are kept, apart from the guide's and the chat page's.</summary>
    private const string ChatsKey = "louis-agent.showroom-chats";

    private string? _pageSentFor;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ChatSettings Settings { get; set; } = null!;
    [Inject] private CurrencyState Currency { get; set; } = null!;
    [Inject] private ChatText Text { get; set; } = null!;
    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The mocked website the chat sits in, handed down by it.</summary>
    [CascadingParameter]
    private SiteMock Mock { get; set; } = null!;

    /// <summary>Who answers: the business's identity, by the name the host's catalogue knows.</summary>
    [Parameter, EditorRequired]
    public string Identity { get; set; } = "";

    /// <summary>A first question to ask, shown before there is one.</summary>
    [Parameter]
    public string Hint { get; set; } = "";

    /// <summary>Told of each tool the agent calls as the turn streams, so the demo shows the place it named.</summary>
    [Parameter]
    public EventCallback<Block> ToolCalled { get; set; }

    private ChatRunner Runner { get; set; } = null!;

    /// <summary>The showroom's words, the site's own when it has them.</summary>
    private ShowroomText Words => Services.GetService<ShowroomText>() ?? new ShowroomText();

    /// <summary>The runner over the site's API, on a store of its own, opening every session as the business.</summary>
    protected override void OnInitialized()
    {
        Runner = new ChatRunner(Api, new ChatStore(JS, Settings, ChatsKey), Text, OpenSessionAsync);
        Runner.Changed += Redraw;
        Runner.ToolCalled += OnToolCalled;
    }

    /// <summary>
    /// Sends the visitor's question, with the mock's page when that page hasn't gone with a question yet, read at send
    /// time so it's the website as it stands; a later question from the same page goes without it, so its words go
    /// once, as the guide's do.
    /// </summary>
    private async Task SendAsync()
    {
        if (!Runner.CanSend) return;

        var page = await Mock.ReadPageAsync();
        // On the same page the runner keeps what it holds: nothing, or the page a failed first send gave back.
        if (page?.Url != _pageSentFor) Runner.Page = page;
        _pageSentFor = page?.Url;
        await Runner.SendAsync();
    }

    private Task<string> OpenSessionAsync() => Api.CreateSessionAsync([], identity: Identity);

    /// <summary>A tool the agent called goes to the demo, on the renderer's thread.</summary>
    private void OnToolCalled(Block block) => InvokeAsync(() => ToolCalled.InvokeAsync(block));

    private void Redraw() => InvokeAsync(StateHasChanged);

    /// <summary>Lets go of the runner's events with the chat.</summary>
    public void Dispose()
    {
        Runner.Changed -= Redraw;
        Runner.ToolCalled -= OnToolCalled;
        GC.SuppressFinalize(this);
    }
}
