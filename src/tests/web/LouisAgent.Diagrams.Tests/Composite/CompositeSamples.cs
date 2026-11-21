namespace LouisAgent.Diagrams.Tests.Composite;

/// <summary>
/// A composite structure diagram the tests share (F134.13): a small engine of four parts, with two ways in and one
/// thing it needs. Small enough to reason about, with every part of the kind in it.
/// </summary>
internal static class CompositeSamples
{
    /// <summary>
    /// The engine: a prompt is passed to its turns and a command to its commands; its turns use its guards and
    /// its registry, its guards use its registry too, and its guards need the model from outside.
    /// </summary>
    public const string Engine =
        """
        {
          "kind": "composite",
          "title": "The engine: what it is made of",
          "caption": "One engine, four parts: it passes what it is asked on to the part that does it.",
          "composite": {
            "name": "AgentEngine",
            "kind": "class",
            "aside": "LouisAgent.Core, Agent",
            "tone": "green",
            "parts": [
              { "id": "turns", "type": "TurnRunner", "role": "turns", "note": "one turn on the pipeline" },
              { "id": "guards", "type": "ToolCallGuards", "role": "guards", "note": "stops a cut-off call" },
              { "id": "registry", "type": "ToolRegistry", "role": "registry" },
              { "id": "commands", "type": "UserCommands", "role": "commands", "note": "what the user types" }
            ],
            "ports": [
              { "name": "prompts", "side": "in", "part": "turns", "label": "ProcessPromptAsync" },
              { "name": "typed", "side": "in", "part": "commands", "label": "HandleUserCommand" },
              { "name": "model", "side": "out", "part": "guards", "label": "IChatClient", "tone": "teal" }
            ],
            "connectors": [
              { "from": "turns", "to": "guards", "word": "the pipeline" },
              { "from": "turns", "to": "registry" },
              { "from": "guards", "to": "registry" }
            ]
          }
        }
        """;
}
