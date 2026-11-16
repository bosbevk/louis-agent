using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Site.Components.Architecture.Framework;

/// <summary>The engine's family on the Architecture page: core, the libraries on it, and the developer tools.</summary>
public partial class EngineFamily
{
    /// <summary>Each project of the family, and what it is.</summary>
    private static readonly IReadOnlyList<Term> Terms =
    [
        new(["louis-agent.core"], "The engine: the tool loop, streaming, the skills, the seam the providers plug into, the usage ledger, handing work to other sessions, and the guards around every tool. It ships no agent and no tools."),
        new(["louis-agent.composition"], "The contracts a pack, an identity and the engine agree on. A pack or an identity references only this, never core."),
        new(["louis-agent.choices"], "One parser for every name that is one of a fixed set: a toolset, a role, a provider. Nothing is matched on a loose string."),
        new(["louis-agent.session-stream"], "Reads a session's server-sent events into records, for the engine's sub-sessions, the web apps and the eval runner alike."),
        new(["louis-agent.core.web"], "Web access on core: the browser, the fetcher, the robots policy, search, rendering, and the recorders that make a web eval replayable."),
        new(["louis-agent.core.telemetry"], "Every request traced through OpenTelemetry, and the collector's log sink."),
        new(["louis-agent.core.anthropic", "louis-agent.core.ollama", "louis-agent.core.openai"], "One model provider each: Claude, a local model through Ollama, any server that speaks OpenAI's chat API. A program hands in the ones its agent may talk to."),
        new(["louis-agent.usage-probe"], "In `src/tools`, a developer's tool: what a provider reports as token usage, and what the ledger makes of it."),
        new(["louis-agent.film-export"], "In `src/tools` too: renders films to WebP for the README, frame by frame."),
        new(["louis-agent.doc-diagrams"], "In `src/tools` as well: draws every diagram in the docs from its JSON, as the SVG beside it.")
    ];
}
