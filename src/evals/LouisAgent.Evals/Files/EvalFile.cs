using System.ComponentModel;
using System.Text.Json.Serialization;
using LouisAgent.Composition.Agents;

namespace LouisAgent.Evals.Files;

/// <summary>
/// An eval, as its file holds it: the one agent it proves, by its address, and the conversations a person has with
/// it. The agent brings its own tools (ADR 0117), so the file names none.
/// </summary>
/// <param name="Agent">The agent the eval proves.</param>
/// <param name="Conversations">The conversations a person has with it.</param>
/// <param name="Schema">
/// The path to the schema an editor checks the file against; the reader keeps it and nothing more.
/// </param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed record EvalFile(
    [property: Description("The agent this eval proves, by its address: an identity, or one of its roles.")]
    AgentAddress Agent,
    [property: Description("The conversations a person has with the agent.")]
    IReadOnlyList<Conversation> Conversations,
    [property: JsonPropertyName("$schema")]
    [property: Description("The path to this file's schema, for the editor.")]
    string? Schema = null);
