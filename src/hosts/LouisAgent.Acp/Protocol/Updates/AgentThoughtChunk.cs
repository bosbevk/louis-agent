namespace LouisAgent.Acp.Protocol.Updates;

/// <summary>A piece of the model's thinking, which Rider shows apart from the answer.</summary>
internal sealed record AgentThoughtChunk(TextBlock Content) : SessionUpdate;
