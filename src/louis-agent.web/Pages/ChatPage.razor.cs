using LouisAgent.Web.Components.Git;
using LouisAgent.Web.Git;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Text;
using LouisAgent.Web.Shared.Usage;
using LouisAgent.Web.Workspace;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ChatModel = LouisAgent.Web.Shared.Chats.Chat;

namespace LouisAgent.Web.Pages;

/// <summary>The chat page's own state and handlers (W2): what the viewers show, and what each action does to it.</summary>
public partial class ChatPage : IDisposable
{
    [Inject] private AgentApi Api { get; set; } = default!;
    [Inject] private ChatStore Store { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private GitState Git { get; set; } = default!;
    [Inject] private GitHistory History { get; set; } = default!;
    [Inject] private CurrencyState Currency { get; set; } = default!;
    [Inject] private ChatText Text { get; set; } = default!;

    [Parameter] public string? SessionId { get; set; }

    /// <summary>A turn's life (W2): the draft, the session, the stream, the save; the page keeps the layout and its viewers.</summary>
    private ChatRunner Runner { get; set; } = default!;

    private WorkspaceFile? _openFile;
    private GitDiff? _openDiff;
    private string? _openDiffKey;
    private GitBranchDetail? _openBranch;
    private GitCommitDetail? _openCommit;
    private string? _openHistoryKey;

    private bool ViewerOpen => _openFile is not null || _openDiff is not null || _openBranch is not null || _openCommit is not null;

    protected override async Task OnInitializedAsync()
    {
        // The code-review app gets every toolset this API can load (F5).
        Runner = new ChatRunner(Api, Store, Text, async () => await Api.CreateSessionAsync(await Api.AvailableToolsetsAsync()));
        Runner.Changed += OnGitChanged;
        Runner.SessionOpened += id => Nav.NavigateTo($"chat/{id}");
        // The agent may have edited files or committed; keep the Changes and Branches badges and panels current.
        Runner.TurnEnded += () => { _ = Git.RefreshAsync(); _ = History.RefreshAsync(); };
        Git.Changed += OnGitChanged;
        History.Changed += OnGitChanged;
        Currency.Changed += OnGitChanged;
        if (!Store.Loaded) await Store.LoadAsync();
        _ = Git.RefreshAsync();
        _ = History.RefreshAsync();
    }

    protected override void OnParametersSet()
    {
        var chat = Store.Find(SessionId);
        Runner.Chat = chat;
        if (SessionId is not null && chat is null && !Runner.Streaming)
            Runner.Notice = "That chat isn't saved in this browser.";
    }

    private void NewChat()
    {
        Runner.Notice = null;
        _openFile = null;
        Nav.NavigateTo("");
    }

    private async Task OpenFile(string path)
    {
        try
        {
            _openDiff = null;
            _openDiffKey = null;
            _openFile = await Api.ReadFileAsync(path);
            CloseHistory();
            Runner.Notice = null;
        }
        catch (Exception ex)
        {
            Runner.Notice = Runner.Describe(ex);
        }
    }

    // Closing a viewer shows the chat again; the message list scrolls to its end when it comes back into view.
    private void CloseFile()
    {
        _openFile = null;
        _openDiff = null;
        _openDiffKey = null;
        CloseHistory();
    }

    private void CloseHistory()
    {
        _openBranch = null;
        _openCommit = null;
        _openHistoryKey = null;
    }

    private async Task OpenBranch(string name)
    {
        try
        {
            var detail = await Api.GitBranchAsync(name);
            CloseFile();
            _openBranch = detail;
            _openHistoryKey = BranchesPanel.BranchKey(name);
            Runner.Notice = null;
        }
        catch (Exception ex)
        {
            Runner.Notice = Runner.Describe(ex);
        }
    }

    private async Task OpenCommit(string sha)
    {
        try
        {
            var detail = await Api.GitShowCommitAsync(sha);
            CloseFile();
            _openCommit = detail;
            _openHistoryKey = BranchesPanel.CommitKey(sha);
            Runner.Notice = null;
        }
        catch (Exception ex)
        {
            Runner.Notice = Runner.Describe(ex);
        }
    }

    // After a merge the branch has nothing left to show, so the viewer closes; the panel says what was merged. The action
    // may wait in the queue behind others, so the outcome is judged by this branch alone, and the viewer only closes if
    // it still shows this branch.
    private async Task MergeBranch(string name)
    {
        await History.MergeAsync(name);
        bool merged = History.Branches?.Branches.FirstOrDefault(b => b.Name == name) is null or { Merged: true };
        if (!merged) Runner.Notice = History.Error ?? $"{name} was not merged.";
        else if (_openBranch?.Branch.Name == name) CloseFile();
    }

    private async Task DeleteBranch(string name)
    {
        await History.DeleteAsync(name);
        bool deleted = History.Branches?.Branches.All(b => b.Name != name) ?? false;
        if (!deleted) Runner.Notice = History.Error ?? $"{name} was not deleted.";
        else if (_openBranch?.Branch.Name == name) CloseFile();
    }

    // A review gets its own chat (ReviewActions), so it doesn't mix with whatever the current chat is about.
    private async Task ReviewFile()
    {
        if (_openFile is null || Runner.Streaming) return;
        var (prompt, title) = ReviewActions.File(_openFile.Path);
        CloseFile();
        Runner.Chat = null;
        await Runner.StartTurnAsync(prompt, [], title);
    }

    private async Task AskAboutFile()
    {
        if (_openFile is null) return;
        Runner.Draft = $"About `{_openFile.Path}`: ";
        CloseFile();
        StateHasChanged();
        await JS.InvokeVoidAsync("louisChat.focusComposer");
    }

    private async Task OpenDiff((GitFile File, bool Staged) change)
    {
        try
        {
            _openDiff = await Api.GitDiffAsync(change.File.Path, change.Staged);
            _openDiffKey = ChangesPanel.Key(change.File, change.Staged);
            _openFile = null;
            CloseHistory();
            Runner.Notice = null;
        }
        catch (Exception ex)
        {
            Runner.Notice = Runner.Describe(ex);
        }
    }

    // After staging, unstaging or discarding the open file, show what is left of it: the same side if it still has
    // changes there, otherwise the other side (e.g. staged after Stage), otherwise close the viewer.
    private async Task ChangeOpenDiff(Func<IEnumerable<string>, Task> change)
    {
        if (_openDiff is null) return;
        string path = _openDiff.Path;
        bool staged = _openDiff.Staged;
        await change([path]);
        if (Git.Error is not null)
        {
            Runner.Notice = Git.Error;
            return;
        }

        var file = Git.Status?.Files.FirstOrDefault(f => f.Path == path);
        if (file is not null && (staged ? file.Staged : file.Unstaged)) await OpenDiff((file, staged));
        else if (file is not null) await OpenDiff((file, !staged));
        else CloseFile();
    }

    private async Task ReviewOpenDiff()
    {
        if (_openDiff is null || Runner.Streaming) return;
        var (prompt, title) = ReviewActions.Diff(_openDiff.Path, _openDiff.Staged, _openDiff.Untracked);
        CloseFile();
        Runner.Chat = null;
        await Runner.StartTurnAsync(prompt, [], title);
    }

    private async Task ReviewAllChanges()
    {
        if (Runner.Streaming) return;
        var (prompt, title) = ReviewActions.AllChanges();
        CloseFile();
        Runner.Chat = null;
        await Runner.StartTurnAsync(prompt, [], title);
    }

    private void OnGitChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Runner.Changed -= OnGitChanged;
        Git.Changed -= OnGitChanged;
        History.Changed -= OnGitChanged;
        Currency.Changed -= OnGitChanged;
    }

    private async Task DeleteChat(ChatModel chat)
    {
        if (await Runner.DeleteChatAsync(chat)) Nav.NavigateTo("");
    }

    private async Task SaveApiKey(string? key) => await Runner.SaveApiKeyAsync(key);
}
