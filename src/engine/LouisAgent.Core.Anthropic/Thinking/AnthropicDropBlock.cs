using System.Text.Json;
using Anthropic.Models.Messages;
using LouisAgent.Core.Llm.Providers;
using LouisAgent.Core.Llm.Thinking;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic;

/// <summary>
/// Anthropic's way with a chat resumed on tools a release moved: its saved thinking is sent as it was, and each of its
/// requests asks the API to drop the thinking blocks that fail the prefix check instead of refusing the request. The
/// model keeps what still matches; the API reports what it dropped.
/// </summary>
/// <remarks>
/// Strategy (Gang of Four), Anthropic's <see cref="IStaleThinking"/>, chosen by <see cref="AnthropicProvider"/> for the
/// models that take the setting. It rides on the regular Messages request through its raw data, the SDK's way of
/// sending a field it hasn't typed there: the beta header <c>thinking-binding-controls-2026-08-01</c> and
/// <c>thinking.block_binding.prefix_mismatch_behavior: "drop_block"</c>. Once a request names its own thinking, the
/// adapter stops adding the turn's effort and display, so they are carried here too. A paid proof on Haiku 5.5 showed
/// the same request a 400 without it and answered with it, the API reporting <c>thinking_dropped</c>.
/// </remarks>
/// <param name="model">The model the requests go to, which the raw request has to name.</param>
/// <seealso href="https://platform.claude.com/docs/en/build-with-claude/preserved-thinking/prefix-checks">Anthropic: prefix checks</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
public sealed class AnthropicDropBlock(string model) : IStaleThinking
{
    /// <summary>The beta that adds the binding setting, by the header that names it.</summary>
    private const string BetaHeader = "anthropic-beta";

    /// <summary>The beta's name in that header.</summary>
    private const string Beta = "thinking-binding-controls-2026-08-01";

    /// <summary>
    /// The models Anthropic lists as taking the setting; any other gets the default, which strips the thinking.
    /// </summary>
    private static readonly string[] Takers =
    [
        "claude-fable-5-2", "claude-mythos-5-2", "claude-fable-5-1", "claude-mythos-5-1", "claude-opus-5-5",
        "claude-sonnet-5-5", "claude-haiku-5-5"
    ];

    /// <summary>Whether the model takes the binding setting.</summary>
    public static bool Supports(string? model) =>
        model is not null && Takers.Any(taker => model.StartsWith(taker, StringComparison.Ordinal));

    /// <summary>The saved turns, thinking and all: the API drops what no longer matches, not us.</summary>
    public IReadOnlyList<ChatMessage> Restore(IReadOnlyList<ChatMessage> saved) => saved;

    /// <summary>
    /// The request with the beta header and drop_block, its effort and display carried; a request with thinking off
    /// is left alone, since the API refuses the setting there.
    /// </summary>
    public ChatOptions Prepare(ChatOptions options)
    {
        if (options.Reasoning?.Effort is not { } effort || effort == ReasoningEffort.None) return options;

        var prepared = options.Clone();
        var previousFactory = prepared.RawRepresentationFactory;
        int maxTokens = options.MaxOutputTokens ?? LlmLimits.MaxOutputTokens;
        string display = options.Reasoning.Output == ReasoningOutput.None ? "omitted" : "summarized";
        prepared.RawRepresentationFactory = client =>
        {
            var before = previousFactory?.Invoke(client) as MessageCreateParams;
            var headers =
                new Dictionary<string, JsonElement>(before?.RawHeaderData ?? new Dictionary<string, JsonElement>())
                {
                    [BetaHeader] = Json(Beta)
                };
            var body = new Dictionary<string, JsonElement>(before?.RawBodyData ?? new Dictionary<string, JsonElement>())
            {
                ["output_config"] = Json(new Dictionary<string, string> { ["effort"] = Effort(effort) })
            };
            body.TryAdd("model", Json(model));
            body.TryAdd("max_tokens", Json(maxTokens));
            body.TryAdd("messages", Json(Array.Empty<object>()));
            var query = before?.RawQueryData ?? new Dictionary<string, JsonElement>();
            return MessageCreateParams.FromRawUnchecked(headers, query, body) with { Thinking = Thinking(display) };
        };
        return prepared;
    }

    /// <summary>Adaptive thinking, shown as asked, with failing blocks dropped.</summary>
    private static ThinkingConfigAdaptive Thinking(string display) => new(new Dictionary<string, JsonElement>
    {
        ["type"] = Json("adaptive"),
        ["display"] = Json(display),
        ["block_binding"] = Json(new Dictionary<string, string> { ["prefix_mismatch_behavior"] = "drop_block" })
    });

    /// <summary>The effort as Anthropic spells it; its scale has one more step above ours.</summary>
    private static string Effort(ReasoningEffort effort) => effort switch
    {
        ReasoningEffort.Low => "low",
        ReasoningEffort.Medium => "medium",
        ReasoningEffort.High => "high",
        ReasoningEffort.ExtraHigh => "xhigh",
        _ => throw new ArgumentOutOfRangeException(nameof(effort), effort, null)
    };

    /// <summary>A value as the raw request's JSON.</summary>
    private static JsonElement Json<T>(T value) => JsonSerializer.SerializeToElement(value);
}
