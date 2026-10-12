namespace LouisAgent.Core.Config;

using LouisAgent.Core.Messages;
using Microsoft.Extensions.AI;

/// <summary>
/// Which LLM to talk to. Changing <see cref="Provider"/>/<see cref="Model"/> is all that is needed to switch backends.
/// </summary>
public sealed class LlmOptions
{
    public const string Anthropic = "anthropic";
    public const string Ollama = "ollama";
    public const string OpenAiCompatible = "openai-compatible";

    // Models known to reject tool definitions or to answer with tool-call JSON as text (qwen2); everything else is assumed to support them unless overridden.
    private static readonly string[] KnownNoToolsPrefixes = ["llama2", "codellama", "deepseek-r1", "deepseek-coder", "qwen2"];

    /// <summary>anthropic | ollama | openai-compatible</summary>
    public string Provider { get; set; } = Anthropic;

    public string Model { get; set; } = "claude-haiku-5-5";

    /// <summary>Base URL. Defaults per provider when empty.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Never logged; see <see cref="ToString"/>.</summary>
    public string? ApiKey { get; set; }

        /// <summary>Anthropic workspace to bill/route to; required when the API key is not scoped to a workspace.</summary>
    public string? WorkspaceId { get; set; }

    /// <summary>Explicit override for tool support; null means infer from provider/model.</summary>
    public bool? SupportsTools { get; set; }

    /// <summary>LLM_THINKING: off | low | medium | high. Null means medium for Anthropic and off for other providers.</summary>
    public string? Thinking { get; set; }

    /// <summary>How hard the model should reason before answering; null disables thinking.</summary>
    public ReasoningEffort? ResolveThinking() =>
        (Thinking ?? (Provider.Equals(Anthropic, StringComparison.OrdinalIgnoreCase) ? "medium" : "off")).ToLowerInvariant() switch
        {
            "low" => ReasoningEffort.Low,
            "medium" => ReasoningEffort.Medium,
            "high" => ReasoningEffort.High,
            "off" or "none" or "false" => null,
            var other => throw new ArgumentException(MessageText.Format(ErrorMessage.UnknownThinking, new { Value = other })),
        };

    /// <summary>LLM_CACHE: on | off. Null means on. Only providers with a caching strategy act on it (F4: Anthropic).</summary>
    public string? Cache { get; set; }

    /// <summary>Whether to mark requests for the provider's prompt cache (F4).</summary>
    public bool ResolveCache() => (Cache ?? "on").ToLowerInvariant() switch
    {
        "on" or "true" => true,
        "off" or "false" => false,
        var other => throw new ArgumentException(MessageText.Format(ErrorMessage.UnknownCacheSetting, new { Value = other })),
    };

    /// <summary>
    /// LLM_CACHE_TTL: 5m | 1h. Null means 5m. The lifetime of the cached prefix (tools + system prompt, F4's marker 1),
    /// chosen from the measured gaps between requests that share it; the conversation's own entry (marker 2) is always
    /// 5 minutes.
    /// </summary>
    public string? CacheTtl { get; set; }

    /// <summary>The prefix's cache lifetime (F4).</summary>
    public CacheLifetime ResolveCacheTtl() => (CacheTtl ?? "5m").ToLowerInvariant() switch
    {
        "5m" => CacheLifetime.FiveMinutes,
        "1h" => CacheLifetime.OneHour,
        var other => throw new ArgumentException(MessageText.Format(ErrorMessage.UnknownCacheTtl, new { Value = other })),
    };

    public bool ResolveSupportsTools()
    {
        if (SupportsTools is { } explicitValue) return explicitValue;
        if (Provider.Equals(Ollama, StringComparison.OrdinalIgnoreCase))
        {
            return !KnownNoToolsPrefixes.Any(p => Model.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        return true;
    }

    /// <summary>
    /// Reads LLM_PROVIDER, LLM_MODEL, LLM_ENDPOINT, LLM_API_KEY, LLM_SUPPORTS_TOOLS.
    /// When no provider is set it is inferred from the model name (claude* is anthropic, else ollama) so older .env files keep working.
    /// </summary>
    public static LlmOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();

        var options = new LlmOptions();
        string? provider = Get("LLM_PROVIDER");
        string? model = Get("LLM_MODEL");
        if (model is not null) options.Model = model;

        options.Provider = (provider ?? (options.Model.StartsWith("claude", StringComparison.OrdinalIgnoreCase)
            ? Anthropic
            : model is null ? Anthropic : Ollama)).ToLowerInvariant();
        options.Endpoint = Get("LLM_ENDPOINT");
        options.ApiKey = Get("LLM_API_KEY")
                         ?? (options.Provider == Anthropic ? Get("ANTHROPIC_API_KEY") : null);
        options.WorkspaceId = Get("ANTHROPIC_WORKSPACE_ID");
        if (bool.TryParse(Get("LLM_SUPPORTS_TOOLS"), out bool supportsTools)) options.SupportsTools = supportsTools;
        options.Thinking = Get("LLM_THINKING");
        options.Cache = Get("LLM_CACHE");
        options.CacheTtl = Get("LLM_CACHE_TTL");
        return options;
    }

    public override string ToString() =>
        $"{Provider}:{Model} (endpoint={Endpoint ?? "default"}, apiKey={(string.IsNullOrEmpty(ApiKey) ? "unset" : "set")})";
}
