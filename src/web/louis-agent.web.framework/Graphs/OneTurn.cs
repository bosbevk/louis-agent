using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Web.Framework.Graphs;

/// <summary>
/// One turn through every layer (F161.1, step 10): your message to the API, the engine's call, the pipeline's tool
/// loop with the model and a pack's tool, the guard on a large result, the ledger, and the answer streamed back.
/// </summary>
/// <remarks>
/// A sequence for the diagram engine, written as the JSON its reader takes, drawn by its own renderer on the page
/// (ADR 0118). Each name is the code's: the route in <c>SessionEndpoints</c>, <c>StreamPromptAsync</c>, the 10 rounds
/// of <c>ChatPipeline</c>, the 50,000-character guard of <c>ToolCallGuards</c>, the ledger of <c>UsageLedger</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public static class OneTurn
{
    /// <summary>The picture, as the engine reads it.</summary>
    public static readonly DiagramSource Source = DiagramReader.Read("""
        {
          "kind": "sequence",
          "title": "One turn, through every layer",
          "sequence": {
            "participants": [
              { "id": "you", "label": "You", "actor": true, "tone": "blue" },
              { "id": "api", "label": "louis-agent.api", "icon": "server", "tone": "violet" },
              { "id": "engine", "label": "AgentEngine", "icon": "chip", "tone": "violet" },
              { "id": "pack", "label": "A pack's tool", "icon": "box", "tone": "green" },
              { "id": "model", "label": "The model", "icon": "cloud", "tone": "red" }
            ],
            "steps": [
              { "step": "message", "from": "you", "to": "api", "text": "POST /v1/sessions/{id}/messages" },
              { "step": "message", "from": "api", "to": "engine", "text": "StreamPromptAsync, with the history" },
              { "step": "block", "block": "loop", "label": "up to 10 tool rounds", "steps": [
                { "step": "message", "from": "engine", "to": "model", "text": "the request, with the tools" },
                { "step": "reply", "from": "model", "to": "engine", "text": "text, or a tool call" },
                { "step": "block", "block": "opt", "label": "a tool call", "steps": [
                  { "step": "message", "from": "engine", "to": "pack", "text": "run it, through the guards" },
                  { "step": "reply", "from": "pack", "to": "engine", "text": "its result" },
                  { "step": "block", "block": "opt", "label": "over 50,000 characters", "steps": [
                    { "step": "message", "from": "engine", "to": "model", "text": "summarise it" }
                  ] }
                ] }
              ] },
              { "step": "note", "over": ["engine", "model"], "text": "every request priced in the ledger" },
              { "step": "reply", "from": "engine", "to": "api", "text": "the answer, as it is written" },
              { "step": "reply", "from": "api", "to": "you", "text": "server-sent events" }
            ],
            "numbered": false
          }
        }
        """);
}
