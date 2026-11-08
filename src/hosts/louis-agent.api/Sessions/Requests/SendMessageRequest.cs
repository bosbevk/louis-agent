using LouisAgent.Api.Sessions.Requests.Pages;

namespace LouisAgent.Api.Sessions.Requests;

internal sealed record SendMessageRequest(string? Message, List<AttachmentRequest>? Attachments, PageRequest? Page = null);
