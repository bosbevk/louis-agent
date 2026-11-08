namespace LouisAgent.Api.Sessions.Requests.Pages;

/// <summary>One named area of the page the visitor is reading (F117): a section's id and its heading.</summary>
internal sealed record PageAreaRequest(string? Id, string? Heading);
