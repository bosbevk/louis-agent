using LouisAgent.Core.Hosting.EnvFiles;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;
using Anthropic.Models.Messages;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Hosting;
using LouisAgent.UsageProbe.Messages;

// Prints what a provider's adapter reports in UsageDetails, streaming and not, and what its IUsageMapper makes of it.
// Run it before trusting the usage ledger for a new provider or after upgrading an adapter (F1 step 1).
//
//   dotnet run --project tools/louis-agent.usage-probe [-- --lines N]
//   dotnet run --project tools/louis-agent.usage-probe -- --think     (2 requests that need reasoning: is thinking text returned?)
//   dotnet run --project tools/louis-agent.usage-probe -- --ledger <usage-YYYY-MM.jsonl>   (price and total a ledger; no model call)
//   dotnet run --project tools/louis-agent.usage-probe -- --cache-gaps <usage-YYYY-MM.jsonl>   (which LLM_CACHE_TTL; F4; no model call)
//   dotnet run --project tools/louis-agent.usage-probe -- --host-culture [en-ZA]   (write the PC's culture to config/.env; F14)
//
// Uses LLM_PROVIDER / LLM_MODEL / LLM_ENDPOINT from config/.env; variables set in the shell win. Every request goes to the
// real provider, so with Anthropic this costs money (about $0.05 on Haiku 4.5 with the default prompt). --ledger only
// reads the file and the price table (PRICES_URL / PRICES_FILE / config/prices.json).

HostEnvironment.Load();
// Read before the line below makes it invariant: --host-culture writes this machine's culture (the Windows region's).
System.Globalization.CultureInfo machineCulture = System.Globalization.CultureInfo.CurrentCulture;
// The same numbers on every machine ($0.0546, 531,265), whatever its locale.
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

if (Array.IndexOf(args, "--host-culture") is var cultureArg and >= 0)
{
    string? given = cultureArg + 1 < args.Length && !args[cultureArg + 1].StartsWith("--") ? args[cultureArg + 1] : null;
    Environment.Exit(WriteHostCulture(given ?? machineCulture.Name));
}

if (Array.IndexOf(args, "--ledger") is var ledgerArg and >= 0)
{
    if (ledgerArg + 1 >= args.Length)
    {
        Console.Error.WriteLine(MessageText.Format(ProbeUserText.LedgerNeedsFile));
        return 2;
    }

    PrintLedger(args[ledgerArg + 1], PriceTableLoader.Load(AgentOptions.FromEnvironment()));
    return 0;
}

if (Array.IndexOf(args, "--cache-gaps") is var gapsArg and >= 0)
{
    if (gapsArg + 1 >= args.Length)
    {
        Console.Error.WriteLine(MessageText.Format(ProbeUserText.CacheGapsNeedsFile));
        return 2;
    }

    PrintCacheGaps(args[gapsArg + 1], PriceTableLoader.Load(AgentOptions.FromEnvironment()));
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
    Console.Error.WriteLine(MessageText.Format(ProbeUserText.ModelNotPulled, new { llm.Model, Endpoint = llm.Endpoint ?? "http://localhost:11434" }));
    Console.Error.WriteLine(MessageText.Format(ProbeUserText.PullInCompose, new { llm.Model }));
    Console.Error.WriteLine(MessageText.Format(ProbeUserText.PullNative, new { llm.Model }));
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
// F14: the containers can't see the PC's region, so its culture is written once into config/.env, which they all read.
static int WriteHostCulture(string name)
{
    if (name.Length == 0)
    {
        Console.Error.WriteLine(MessageText.Format(ProbeUserText.NoMachineCulture));
        return 1;
    }

    System.Globalization.RegionInfo region;
    try
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo(name);
        if (culture.IsNeutralCulture) throw new ArgumentException(MessageText.Format(ProbeUserText.LanguageWithoutRegion));
        region = new System.Globalization.RegionInfo(culture.Name);
    }
    catch (Exception ex) when (ex is ArgumentException or System.Globalization.CultureNotFoundException)
    {
        Console.Error.WriteLine(MessageText.Format(ProbeUserText.CultureNamesNoRegion, new { Name = name, Reason = ex.Message }));
        return 1;
    }

    var agent = AgentOptions.FromEnvironment();
    agent.HostCulture = name;
    agent.DisplayCurrency = null;
    PriceTable prices = PriceTableLoader.Load(agent);
    ExchangeRates rates = ExchangeRatesLoader.Load(agent, prices.Currency);
    DisplayCurrency display = DisplayCurrency.Resolve(agent, prices.Currency, rates);

    Console.WriteLine(MessageText.Format(ProbeUserText.CultureFound, new { Name = name, Region = region.TwoLetterISORegionName, Code = region.ISOCurrencySymbol, Symbol = region.CurrencySymbol }));
    Console.WriteLine(display.Converts
        ? MessageText.Format(ProbeUserText.RateFound, new { display.Base, display.Rate, display.Code, AsOf = display.RateAsOf, rates.Source })
        : MessageText.Format(ProbeUserText.ShownAsCharged, new { display.Base }));

    if (EnvFile.Find(Environment.CurrentDirectory) is not { } envFile)
    {
        Console.Error.WriteLine(MessageText.Format(ProbeUserText.NoEnvFile));
        return 1;
    }

    bool replaced = EnvFile.Write(envFile, "HOST_CULTURE", name,
        MessageText.Format(ProbeUserText.HostCultureComment));
    Console.WriteLine(MessageText.Format(replaced ? ProbeUserText.HostCultureUpdated : ProbeUserText.HostCultureAdded, new { Name = name, File = envFile }));
    if (Environment.GetEnvironmentVariable("DISPLAY_CURRENCY") is { Length: > 0 } other)
        Console.WriteLine(MessageText.Format(ProbeUserText.DisplayCurrencyOverrides, new { Code = other }));
    Console.WriteLine(MessageText.Format(ProbeUserText.RestartHosts));
    return 0;
}

// F4-S4: the gaps between requests that share the prefix, and what each lifetime would have cost on them.
static void PrintCacheGaps(string path, PriceTable prices)
{
    IReadOnlyList<CacheGapGroup> groups = CacheGaps.Analyse(UsageReport.Read(path), prices);
    Console.WriteLine(MessageText.Format(ProbeUserText.CacheGapsHeader, new { Path = path, AsOf = prices.AsOf ?? "(none)" }));
    if (groups.Count == 0) Console.WriteLine(MessageText.Format(ProbeUserText.CacheGapsNone, new { Path = path }));

    foreach (CacheGapGroup g in groups)
    {
        Console.WriteLine();
        Console.WriteLine(MessageText.Format(ProbeUserText.CacheGapsGroup, new { g.Host, g.Model, g.Requests, Prefix = g.PrefixTokens.ToString("N0") }));
        Console.WriteLine(MessageText.Format(ProbeUserText.CacheGapsGaps, new { g.Under5m, g.From5To60m, g.Over60m }));
        foreach (var (lifetime, cost) in new[] { ("5 minutes", g.FiveMinutes), ("1 hour   ", g.OneHour) })
            Console.WriteLine(MessageText.Format(ProbeUserText.CacheGapsLifetime,
                new { Lifetime = lifetime, cost.Writes, cost.Reads, Cost = cost.Cost is { } c ? Money(c) : "price unknown" }));

        if (g is not { FiveMinutes.Cost: { } five, OneHour.Cost: { } hour })
            Console.WriteLine(MessageText.Format(ProbeUserText.CacheGapsNoPrice, new { g.Model }));
        else if (g.Recommended == CacheLifetime.OneHour)
            Console.WriteLine(MessageText.Format(ProbeUserText.CacheGapsRecommendOneHour,
                new { Saving = Money(five - hour), Percent = (five - hour) / five is var share ? share.ToString("0%") : "" }));
        else
            Console.WriteLine(MessageText.Format(ProbeUserText.CacheGapsRecommendFiveMinutes, new { Extra = Money(hour - five) }));
    }

    string Money(decimal amount) => prices.Currency == "USD" ? $"${amount:0.0000}" : $"{amount:0.0000} {prices.Currency}";
}

static void PrintLedger(string path, PriceTable prices)
{
    var rows = UsageReport.Price(UsageReport.Read(path), prices).ToList();
    // A sub-session's roll-up (F5) stands for requests that are rows of their own in the ledger they ran on (this one,
    // when peers share LOG_DIRECTORY as Puls and its specialist do), so it is listed, not totalled: a turn's own line
    // counts it; a ledger's totals count the requests.
    var rollUps = rows.Where(r => r.Purpose == UsagePurpose.SubSession).ToList();
    var records = rows.Except(rollUps).ToList();
    var models = records.Select(r => r.Model).Distinct().ToList();
    Console.WriteLine(MessageText.Format(ProbeUserText.LedgerSummary, new { Path = path, records.Count, Models = string.Join(", ", models), AsOf = prices.AsOf ?? "(none)" }));
    if (rollUps.Count > 0)
    {
        UsageTotal delegated = UsageReport.Total("sub_session", rollUps);
        Console.WriteLine(MessageText.Format(ProbeUserText.LedgerRollUps, new
        {
            Rows = rollUps.Count, delegated.Requests,
            Cost = delegated.Cost is { } cost ? $"${cost:0.0000}" : "unknown",
        }));
    }

    Section(MessageText.Format(ProbeUserText.SectionPerWork), UsageReport.Totals(records, UsageReport.WorkOf));
    Section(MessageText.Format(ProbeUserText.SectionPerPurpose), UsageReport.Totals(records, r => r.Purpose));
    Section(MessageText.Format(ProbeUserText.SectionPerRun), UsageReport.Totals(records, r => r.Run ?? "(no run)"));
    Section(MessageText.Format(ProbeUserText.SectionAll), [UsageReport.Total("all", records)]);

    void Section(string title, IReadOnlyList<UsageTotal> totals)
    {
        Console.WriteLine($"\n{title}");
        Console.WriteLine($"  {"",-34} {"requests",8} {"input",11} {"cache w",9} {"cache r",9} {"output",8} {"cost",10}");
        foreach (UsageTotal t in totals)
            Console.WriteLine($"  {t.Key,-34} {t.Requests,8} {t.Input,11:N0} {t.CacheWrite,9:N0} {t.CacheRead,9:N0} {t.Output,8:N0} " +
                              $"{(t.Cost is { } cost ? $"${cost:0.0000}" : "unknown"),10}{(t.Unpriced > 0 ? $"  ({t.Unpriced} unpriced)" : "")}");
    }
}
