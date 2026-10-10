using Anthropic.Models.Messages;
using louis_agent.core;
using louis_agent.core.config;
using louis_agent.core.providers;
using louis_agent.core.usage;
using Microsoft.Extensions.AI;

// Prints what a provider's adapter reports in UsageDetails, streaming and not, and what its IUsageMapper makes of it.
// Run it before trusting the usage ledger for a new provider or after upgrading an adapter (F1 step 1).
//
//   dotnet run --project tools/louis-agent.usage-probe [-- --lines N]
//
// Uses LLM_PROVIDER / LLM_MODEL / LLM_ENDPOINT from config/.env; variables set in the shell win. Every request goes to the
// real provider, so with Anthropic this costs money (about $0.05 on Haiku 4.5 with the default prompt).

AgentHost.LoadEnvironment();
var llm = LlmOptions.FromEnvironment();
var factory = new LlmClientFactory();
IChatClient client = factory.Create(llm);
IUsageMapper mapper = factory.CreateUsageMapper(llm);
ReasoningEffort? thinking = llm.ResolveThinking();
bool anthropic = string.Equals(llm.Provider, LlmOptions.Anthropic, StringComparison.OrdinalIgnoreCase);

// Anthropic only caches a prefix above a minimum (4,096 tokens on Haiku 4.5); small local models have small context
// windows, so they get a short prompt.
int lines = args.Length == 2 && args[0] == "--lines" ? int.Parse(args[1]) : anthropic ? 600 : 40;
string filler = string.Join("\n", Enumerable.Range(1, lines).Select(i => $"Reference line {i}: the quick brown fox jumps over the lazy dog."));
string prompt = NewPrompt();

Console.WriteLine($"provider={llm.Provider} model={llm.Model} thinking={thinking?.ToString() ?? "off"} mapper={mapper.GetType().Name} lines={lines}");

await NonStreaming("1 non-streaming", cache: false);
await Streaming("2 streaming", cache: false);
if (anthropic)
{
    await NonStreaming("3 non-streaming, cache marker (expect write)", cache: true);
    await NonStreaming("4 non-streaming, cache marker (expect read)", cache: true);
    prompt = NewPrompt();
    await Streaming("5 streaming, cache marker (expect write)", cache: true);
    await Streaming("6 streaming, cache marker (expect read)", cache: true);
}
else
{
    // No cache markers outside Anthropic; repeating the prompt shows whether the provider reports its own caching.
    await NonStreaming("3 non-streaming, same prompt again", cache: false);
    await Streaming("4 streaming, same prompt again", cache: false);
}

// A per-run nonce keeps runs from reading each other's cache.
string NewPrompt() => $"Run {Guid.NewGuid():N}. You answer in one word.\n{filler}";

ChatOptions Options() => new()
{
    Reasoning = thinking is { } effort ? new ReasoningOptions { Effort = effort, Output = ReasoningOutput.Full } : null,
};

List<ChatMessage> Messages(bool cache)
{
    var system = new TextContent(prompt);
    if (cache) system = system.WithCacheControl(new CacheControlEphemeral());
    return [new ChatMessage(ChatRole.System, [system]), new ChatMessage(ChatRole.User, "Say hello.")];
}

async Task NonStreaming(string label, bool cache)
{
    ChatResponse response = await client.GetResponseAsync(Messages(cache), Options());
    int usageContents = response.Messages.SelectMany(m => m.Contents).OfType<UsageContent>().Count();
    Console.WriteLine($"--- {label}: finish={response.FinishReason} model={response.ModelId} UsageContent items={usageContents}");
    Print("ChatResponse.Usage", response.Usage);
}

async Task Streaming(string label, bool cache)
{
    var updates = new List<ChatResponseUpdate>();
    await foreach (var update in client.GetStreamingResponseAsync(Messages(cache), Options())) updates.Add(update);

    var carriers = updates.Select((u, i) => (Update: u, Index: i))
        .Where(x => x.Update.Contents.OfType<UsageContent>().Any())
        .Select(x => $"{x.Index + 1}{(x.Update.FinishReason is { } f ? $"(finish={f})" : "")}")
        .ToList();
    Console.WriteLine($"--- {label}: {updates.Count} updates; UsageContent in update(s) {(carriers.Count == 0 ? "none" : string.Join(", ", carriers))} of {updates.Count}");
    Print("ToChatResponse().Usage", updates.ToChatResponse().Usage);
}

void Print(string source, UsageDetails? usage)
{
    if (usage is null)
    {
        Console.WriteLine($"  {source}: null");
    }
    else
    {
        Console.WriteLine($"  {source}: Input={Show(usage.InputTokenCount)} Output={Show(usage.OutputTokenCount)} " +
                          $"Total={Show(usage.TotalTokenCount)} CachedInput={Show(usage.CachedInputTokenCount)} " +
                          $"Reasoning={Show(usage.ReasoningTokenCount)}");
        foreach (var (key, value) in usage.AdditionalCounts ?? [])
            Console.WriteLine($"    AdditionalCounts[{key}]={value}");
    }

    UsageTokens tokens = mapper.Map(usage);
    Console.WriteLine($"  ledger: input={Show(tokens.Input)} cache_write={Show(tokens.CacheWrite)} cache_read={Show(tokens.CacheRead)} " +
                      $"output={Show(tokens.Output)} reasoning={Show(tokens.Reasoning)}");
}

static string Show(long? count) => count?.ToString() ?? "null";
