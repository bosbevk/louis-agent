namespace LouisAgent.Api.Sessions.Requests;

internal sealed record SendMessageRequest(string? Message, List<AttachmentRequest>? Attachments);
