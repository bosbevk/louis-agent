namespace LouisAgent.Api.Evals.Messages.Holes.Runs;

/// <summary>The run, and the conversation it doesn't hold.</summary>
public sealed record NoSuchConversationHoles(string Id, string Conversation);
