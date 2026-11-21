namespace LouisAgent.Web.Chats.Calls;

/// <summary>What an Offer call put to the visitor: the question, and the choices drawn as cards under it.</summary>
public sealed record Offered(string Question, IReadOnlyList<OfferChoice> Choices);
