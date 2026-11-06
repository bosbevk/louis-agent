using LouisAgent.Web.Chats;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat;

/// <summary>The message box: the attachments, the text, and Send or Stop.</summary>
public partial class Composer
{
    [Parameter] public string Draft { get; set; } = "";
    [Parameter] public EventCallback<string> DraftChanged { get; set; }
    [Parameter, EditorRequired] public List<Attachment> Attachments { get; set; } = default!;
    [Parameter] public bool Streaming { get; set; }
    [Parameter] public bool CanSend { get; set; }

    /// <summary>True once the chat ended (its session is gone): the box takes no more text.</summary>
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public string? Placeholder { get; set; }

    [Parameter] public EventCallback OnSend { get; set; }
    [Parameter] public EventCallback OnStop { get; set; }

    /// <summary>The class of the box around the text, which each site styles.</summary>
    [Parameter] public string BoxClass { get; set; } = "input-row";

    /// <summary>What a site puts before the text, such as an attach button and a prompt sign.</summary>
    [Parameter] public RenderFragment? BeforeInput { get; set; }

    /// <summary>A site's toolbar after the text; the Send and Stop buttons then close it.</summary>
    [Parameter] public RenderFragment? Tools { get; set; }

    [Parameter] public string SendLabel { get; set; } = "Send";
    [Parameter] public string StopLabel { get; set; } = "Stop";
    [Parameter] public string? SendTitle { get; set; }
    [Parameter] public string? StopTitle { get; set; }
}
