namespace LouisAgent.SessionStream.Wire.Requests;

/// <summary>One named area of the page the visitor is reading (F117): a section's id and its heading.</summary>
public sealed record PageAreaRequest(string? Id, string? Heading);
