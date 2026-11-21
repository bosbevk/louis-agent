namespace LouisAgent.Orchestrator.Client;

/// <summary>One tool louis-agent called while answering: its input (JSON) and the result it got back.</summary>
internal sealed class AgentToolCall(string id, string name, string input)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string Input { get; } = input;
    public string? Result { get; set; }
    public bool IsError { get; set; }
}
