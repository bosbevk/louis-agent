using System.Net;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components.Forms;

namespace LouisAgent.Web.Chats;

/// <summary>
/// A turn's life, the same for every chat page (W2): the draft and its attachments, the session opened on the first
/// message, the stream folded into the turn with a redraw raised no more than every 50 ms, stop, the chat saved at
/// the end, and the errors in the app's words. The page keeps the layout: it hands the runner how to open a session
/// (which toolsets, which role, where the user is), navigates when <see cref="SessionOpened"/> fires, and redraws on
/// <see cref="Changed"/>.
/// </summary>
public sealed class ChatRunner(
    AgentApi api,
    ChatStore store,
    ChatText text,
    Func<Task<string>> openSession,
    TimeProvider? clock = null)
{
    public const long MaxAttachmentBytes = 1_000_000;
    private static readonly TimeSpan RedrawEvery = TimeSpan.FromMilliseconds(50);

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    /// <summary>
    /// The chat the page shows; the page sets it from its route, the runner sets it when a session opens.
    /// </summary>
    public Chat? Chat { get; set; }

    public string Draft { get; set; } = "";
    public List<Attachment> Attachments { get; } = [];

    /// <summary>The page the visitor is reading, sent with the next message and then forgotten (F115).</summary>
    public Page? Page { get; set; }

    /// <summary>
    /// The pages of the agents' windows on the visitor's screen, sent with every message (FX134), so an ask to one of
    /// those agents sends it its page.
    /// </summary>
    public IReadOnlyList<WindowPage>? Windows { get; set; }
    public bool Streaming { get; private set; }
    public string? Notice { get; set; }

    /// <summary>
    /// Raised when the page should redraw: the turn started, text arrived (throttled), the turn ended, files were
    /// attached, or a send or stop failed with a notice (FX29).
    /// </summary>
    public event Action? Changed;

    /// <summary>A new chat's session was opened: the page navigates to it.</summary>
    public event Action<string>? SessionOpened;

    /// <summary>A turn ended, however it ended: a site refreshes whatever the agent may have changed here.</summary>
    public event Action? TurnEnded;

    /// <summary>
    /// A tool call arrived in the turn (F117), once per call, so a page can act on it while the turn still streams.
    /// </summary>
    public event Action<Block>? ToolCalled;

    public bool CanSend => !Streaming && Chat?.Ended != true && (Draft.Trim().Length > 0 || Attachments.Count > 0);

    /// <summary>Sends the draft with its attachments, and clears both.</summary>
    public async Task SendAsync()
    {
        if (!CanSend) return;

        string message = Draft.Trim();
        var attachments = Attachments.ToList();
        var page = Page;
        Page = null;
        Draft = "";
        Attachments.Clear();
        await StartTurnAsync(message, attachments, page);
    }

    /// <summary>
    /// One turn: a session is opened for a new chat (<paramref name="title"/> names it, else its first message does),
    /// the turn is added and streamed, and the chat saved when the stream ends, however it ends.
    /// </summary>
    public async Task StartTurnAsync(string message, List<Attachment> attachments, Page? page = null,
        string? title = null)
    {
        Notice = null;
        if (Chat is null)
        {
            try
            {
                string sessionId = await openSession();
                Chat = new Chat
                {
                    SessionId = sessionId,
                    Title = title ?? TitleFor(message, attachments)
                };
                store.Chats.Insert(0, Chat);
                SessionOpened?.Invoke(sessionId);
            }
            catch (Exception ex)
            {
                // The draft goes back and the page is told to redraw, or the notice never shows (FX29).
                Draft = message;
                Attachments.AddRange(attachments);
                Page ??= page;
                Notice = Describe(ex);
                Changed?.Invoke();
                return;
            }
        }

        var chat = Chat;
        var turn = new Turn
        {
            UserText = message,
            AttachmentNames = attachments.Select(a => a.Name).ToList()
        };
        chat.Turns.Add(turn);
        chat.UpdatedAt = DateTimeOffset.Now;
        Streaming = true;
        Changed?.Invoke();

        // Text arrives a few words at a time; redrawing every 50 ms is smooth, and not on every token.
        var redraw = new RedrawThrottle(_clock, RedrawEvery, () => Changed?.Invoke());
        try
        {
            await foreach (var streamEvent in api.StreamMessageAsync(chat.SessionId, message, attachments, page,
                               Windows))
            {
                // A tool call is one new block the moment it starts; its result later changes that block, adding none.
                int blocks = turn.Blocks.Count;
                TurnStream.Apply(turn, streamEvent);
                if (turn.Blocks.Count > blocks && turn.Blocks[^1] is { Kind: BlockKind.Tool } call)
                    ToolCalled?.Invoke(call);
                redraw.Request();
            }
        }
        catch (AgentApiException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            chat.Ended = true;
            turn.Error = text.SessionGone;
        }
        catch (Exception ex)
        {
            turn.Error = Describe(ex);
        }
        finally
        {
            redraw.Stop();
            Streaming = false;
            TurnEnded?.Invoke();
            await store.SaveAsync();
            Changed?.Invoke();
        }
    }

    /// <summary>Asks the API to cancel the turn; if it refuses, the notice says why.</summary>
    public async Task StopAsync()
    {
        if (Chat is null) return;
        try
        {
            await api.CancelAsync(Chat.SessionId);
        }
        catch (Exception ex)
        {
            // The stop button can sit in a child component; the page is told to redraw so the notice shows (FX29).
            Notice = Describe(ex);
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Attaches the chosen text files, each capped at 1 MB; a larger one is refused with a notice, and the page is
    /// told to redraw so the notice shows (FX29).
    /// </summary>
    public async Task AddFilesAsync(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles(maximumFileCount: 10))
        {
            if (file.Size > MaxAttachmentBytes)
            {
                Notice = string.Format(text.AttachmentTooLarge, file.Name);
                continue;
            }

            using var reader = new StreamReader(file.OpenReadStream(MaxAttachmentBytes));
            Attachments.Add(new Attachment(file.Name, await reader.ReadToEndAsync()));
        }

        Changed?.Invoke();
    }

    /// <summary>Deletes a chat here and on the server; true when it was the one shown, so the page leaves it.</summary>
    public async Task<bool> DeleteChatAsync(Chat chat)
    {
        try
        {
            await api.DeleteSessionAsync(chat.SessionId);
        }
        catch (Exception)
        {
            // The local copy is removed either way.
        }

        store.Chats.Remove(chat);
        await store.SaveAsync();
        bool shown = ReferenceEquals(chat, Chat);
        if (shown) Chat = null;
        return shown;
    }

    /// <summary>Keeps the API key and clears the notice that asked for it.</summary>
    public async Task SaveApiKeyAsync(string? apiKey)
    {
        await store.SaveApiKeyAsync(apiKey);
        Notice = null;
    }

    /// <summary>
    /// A chat's title: its first message on one line, else its first attachment's name, else the app's "New chat".
    /// </summary>
    public string TitleFor(string message, List<Attachment> attachments)
    {
        string title = message.Length > 0
            ? message.ReplaceLineEndings(" ")
            : attachments.FirstOrDefault()?.Name ?? text.NewChat;
        return title.Length > 50 ? title[..50] + "…" : title;
    }

    /// <summary>
    /// An error as the page shows it: the app's words for the ones it knows, the message for the rest.
    /// </summary>
    public string Describe(Exception ex) => ex switch
    {
        AgentApiException { Status: HttpStatusCode.Unauthorized } => text.NeedsKey,
        AgentApiException { Status: HttpStatusCode.Conflict } => text.StillAnswering,
        AgentApiException api => api.Message,
        HttpRequestException => text.ApiUnreachable,
        _ => ex.Message,
    };
}
