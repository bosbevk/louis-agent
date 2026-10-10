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
//   dotnet run --project tools/louis-agent.usage-probe -- --think     (2 requests that need reasoning: is thinking text returned?)
//   dotnet run --project tools/louis-agent.usage-probe -- --ledger <usage-YYYY-MM.jsonl>   (price and total a ledger; no model call)
//
// Uses LLM_PROVIDER / LLM_MODEL / LLM_ENDPOINT from config/.env; variables set in the shell win. Every request goes to the
// real provider, so with Anthropic this costs money (about $0.05 on Haiku 4.5 with the default prompt). --ledger only
// reads the file and the price table (PRICES_URL / PRICES_FILE / config/prices.json).

AgentHost.LoadEnvironment();
// The same numbers on every machine ($0.0546, 531,265), whatever its locale.
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

if (Array.IndexOf(args, "--ledger") is var ledgerArg and >= 0)
{
    if (ledgerArg + 1 >= args.Length)
    {
        Console.Error.WriteLine("--ledger needs a file: --ledger logs/usage-2026-10.jsonl");
        return 2;
    }

    PrintLedger(args[ledgerArg + 1], AgentHost.LoadPrices(AgentOptions.FromEnvironment()));
    return 0;
}

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

if (args.Contains("--think"))
{
    // A question a model won't answer without working it out, so adaptive thinking actually thinks.
    List<ChatMessage> puzzle =
    [
        new(ChatRole.User,
            "Three boxes are labelled 'apples', 'oranges' and 'mixed', and every label is wrong. You may take one fruit " +
            "from one box. Which box do you pick from, and how do you then relabel all three? Answer in two sentences."),
    ];
    ChatResponse response = await client.GetResponseAsync(puzzle, Options());
    Console.WriteLine($"--- think, non-streaming: finish={response.FinishReason}");
    PrintThinking(response.Messages.SelectMany(m => m.Contents));
    Print("ChatResponse.Usage", response.Usage);

    var updates = new List<ChatResponseUpdate>();
    await foreach (var update in client.GetStreamingResponseAsync(puzzle, Options())) updates.Add(update);
    Console.WriteLine($"--- think, streaming: {updates.Count} updates");
    PrintThinking(updates.SelectMany(u => u.Contents));
    Print("ToChatResponse().Usage", updates.ToChatResponse().Usage);
    return 0;
}

try
{
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
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound &&
                                      string.Equals(llm.Provider, LlmOptions.Ollama, StringComparison.OrdinalIgnoreCase))
{
    // Ollama answers 404 for a model it hasn't pulled; the stack trace doesn't say so.
    Console.Error.WriteLine($"Model '{llm.Model}' isn't pulled into Ollama at {llm.Endpoint ?? "http://localhost:11434"}. Pull it with:");
    Console.Error.WriteLine($"  docker exec louis_ollama ollama pull {llm.Model}    (the compose ollama service)");
    Console.Error.WriteLine($"  ollama pull {llm.Model}                             (Ollama installed natively)");
    return 1;
}

return 0;

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
    PrintThinking(response.Messages.SelectMany(m => m.Contents));
    Print("ChatResponse.Usage", response.Usage);
}

// Some models think but return empty thinking text unless asked for a summary; hosts would then show nothing.
void PrintThinking(IEnumerable<AIContent> contents)
{
    var thinking = contents.OfType<TextReasoningContent>().ToList();
    Console.WriteLine($"  thinking: {thinking.Count} block(s), {thinking.Sum(t => t.Text.Length)} chars of text, " +
                      $"{thinking.Count(t => !string.IsNullOrEmpty(t.ProtectedData))} with a signature");
    string text = string.Concat(thinking.Select(t => t.Text)).ReplaceLineEndings(" ");
    if (text.Length > 0) Console.WriteLine($"  thinking starts: {text[..Math.Min(160, text.Length)]}");
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
    PrintThinking(updates.SelectMany(u => u.Contents));
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

// Prices a ledger (records written before F2 have no cost) and totals it per piece of work, per purpose, per run and in all.
static void PrintLedger(string path, PriceTable prices)
{
    var records = UsageReport.Price(UsageReport.Read(path), prices).ToList();
    var models = records.Select(r => r.Model).Distinct().ToList();
    Console.WriteLine($"{path}: {records.Count} requests; models {string.Join(", ", models)}; prices as of {prices.AsOf ?? "(none)"}");

    Section("Per piece of work (a fix's task, a triage or a chat session)", UsageReport.Totals(records, UsageReport.WorkOf));
    Section("Per purpose", UsageReport.Totals(records, r => r.Purpose));
    Section("Per run", UsageReport.Totals(records, r => r.Run ?? "(no run)"));
    Section("All", [UsageReport.Total("all", records)]);

    void Section(string title, IReadOnlyList<UsageTotal> totals)
    {
        Console.WriteLine($"\n{title}");
        Console.WriteLine($"  {"",-34} {"requests",8} {"input",11} {"cache w",9} {"cache r",9} {"output",8} {"cost",10}");
        foreach (UsageTotal t in totals)
            Console.WriteLine($"  {t.Key,-34} {t.Requests,8} {t.Input,11:N0} {t.CacheWrite,9:N0} {t.CacheRead,9:N0} {t.Output,8:N0} " +
                              $"{(t.Cost is { } cost ? $"${cost:0.0000}" : "unknown"),10}{(t.Unpriced > 0 ? $"  ({t.Unpriced} unpriced)" : "")}");
    }
}
