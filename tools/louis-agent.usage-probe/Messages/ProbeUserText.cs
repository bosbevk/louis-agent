namespace LouisAgent.UsageProbe.Messages;

using System.ComponentModel;

/// <summary>The usage probe's text for people: its errors and reports (key=value dumps and table layouts stay as format code).</summary>
public enum ProbeUserText
{
    [Description("{Path}: the prefix's cache lifetime (F4), per host and model; prices as of {AsOf}")]
    CacheGapsHeader,

    [Description("No requests that carry the prefix in {Path}.")]
    CacheGapsNone,

    [Description("{Host} · {Model} · {Requests} requests · prefix ≈ {Prefix} tokens (its smallest prompt)")]
    CacheGapsGroup,

    [Description("  gaps: {Under5m} under 5 min · {From5To60m} from 5 to 60 min · {Over60m} over an hour")]
    CacheGapsGaps,

    [Description("  {Lifetime}: writes {Writes}, reads {Reads} → {Cost}")]
    CacheGapsLifetime,

    [Description("  → LLM_CACHE_TTL=1h: saves {Saving} ({Percent}) on the prefix")]
    CacheGapsRecommendOneHour,

    [Description("  → LLM_CACHE_TTL=5m (the default): 1 hour would cost {Extra} more")]
    CacheGapsRecommendFiveMinutes,

    [Description("  no price for {Model}: no recommendation")]
    CacheGapsNoPrice,

    [Description("--cache-gaps needs a file: --cache-gaps logs/usage-2026-10.jsonl")]
    CacheGapsNeedsFile,

    [Description("--ledger needs a file: --ledger logs/usage-2026-10.jsonl")]
    LedgerNeedsFile,

    [Description("Model '{Model}' isn't pulled into Ollama at {Endpoint}. Pull it with:")]
    ModelNotPulled,

    [Description("  docker exec louis_ollama ollama pull {Model}    (the compose ollama service)")]
    PullInCompose,

    [Description("  ollama pull {Model}                             (Ollama installed natively)")]
    PullNative,

    [Description("This machine has no culture of its own (a container?). Run this on the PC, or name one: --host-culture en-ZA")]
    NoMachineCulture,

    [Description("a language without a region")]
    LanguageWithoutRegion,

    [Description("'{Name}' names no region ({Reason}). Use a culture such as en-ZA.")]
    CultureNamesNoRegion,

    [Description("Culture: {Name} -> region {Region}, currency {Code} ({Symbol})")]
    CultureFound,

    [Description("Rate: 1 {Base} = {Rate} {Code} (as of {AsOf}, {Source})")]
    RateFound,

    [Description("Costs are shown as charged, in {Base}.")]
    ShownAsCharged,

    [Description("No config/.env found: copy config/.env.example to config/.env first.")]
    NoEnvFile,

    [Description("Note: DISPLAY_CURRENCY={Code} is set and overrides it.")]
    DisplayCurrencyOverrides,

    [Description("Restart the hosts to use it: the containers (docker compose ... up -d), and a New Chat in Rider.")]
    RestartHosts,

    [Description("The PC's culture (F14): costs are shown in its region's currency. Written by usage-probe --host-culture.")]
    HostCultureComment,

    [Description("{Path}: {Count} requests; models {Models}; prices as of {AsOf}")]
    LedgerSummary,

    [Description("Per piece of work (a fix's task, a triage or a chat session)")]
    SectionPerWork,

    [Description("Per purpose")]
    SectionPerPurpose,

    [Description("Per run")]
    SectionPerRun,

    [Description("All")]
    SectionAll,

    [Description("Updated HOST_CULTURE={Name} in {File}")]
    HostCultureUpdated,

    [Description("Added HOST_CULTURE={Name} in {File}")]
    HostCultureAdded,
}
