using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.UsageProbe.Messages.Holes.Gaps;
using LouisAgent.UsageProbe.Messages.Holes.Lifetimes;
using LouisAgent.UsageProbe.Messages.Holes.Ollama;
using LouisAgent.UsageProbe.Messages.Holes.Culture;
using LouisAgent.UsageProbe.Messages.Holes.Settings;
using LouisAgent.UsageProbe.Messages.Holes.Ledger;

namespace LouisAgent.UsageProbe.Messages;

/// <summary>
/// The usage probe's text for people: its errors and reports (key=value dumps and table layouts stay as format code).
/// </summary>
public enum ProbeUserText
{
    [Description("{Path}: the prefix's cache lifetime (F4), per host and model; prices as of {AsOf}")]
    [Holes(typeof(CacheGapsHeaderHoles))]
    CacheGapsHeader,

    [Description("No requests that carry the prefix in {Path}.")]
    [Holes(typeof(CacheGapsNoneHoles))]
    CacheGapsNone,

    [Description("{Host} · {Model} · {Requests} requests · prefix ≈ {Prefix} tokens (its smallest prompt)")]
    [Holes(typeof(CacheGapsGroupHoles))]
    CacheGapsGroup,

    [Description("  gaps: {Under5m} under 5 min · {From5To60m} from 5 to 60 min · {Over60m} over an hour")]
    [Holes(typeof(CacheGapsGapsHoles))]
    CacheGapsGaps,

    [Description("  {Lifetime}: writes {Writes}, reads {Reads} → {Cost}")]
    [Holes(typeof(CacheGapsLifetimeHoles))]
    CacheGapsLifetime,

    [Description("  → LLM_CACHE_TTL=1h: saves {Saving} ({Percent}) on the prefix")]
    [Holes(typeof(CacheGapsRecommendOneHourHoles))]
    CacheGapsRecommendOneHour,

    [Description("  → LLM_CACHE_TTL=5m (the default): 1 hour would cost {Extra} more")]
    [Holes(typeof(CacheGapsRecommendFiveMinutesHoles))]
    CacheGapsRecommendFiveMinutes,

    [Description("  no price for {Model}: no recommendation")]
    [Holes(typeof(CacheGapsNoPriceHoles))]
    CacheGapsNoPrice,

    [Description("--cache-gaps needs a file: --cache-gaps logs/usage-2026-10.jsonl")]
    CacheGapsNeedsFile,

    [Description("--ledger needs a file: --ledger logs/usage-2026-10.jsonl")]
    LedgerNeedsFile,

    [Description("Model '{Model}' isn't pulled into Ollama at {Endpoint}. Pull it with:")]
    [Holes(typeof(ModelNotPulledHoles))]
    ModelNotPulled,

    [Description("  docker exec louis_ollama ollama pull {Model}    (the compose ollama service)")]
    [Holes(typeof(PullInComposeHoles))]
    PullInCompose,

    [Description("  ollama pull {Model}                             (Ollama installed natively)")]
    [Holes(typeof(PullNativeHoles))]
    PullNative,

    [Description(
        "This machine has no culture of its own (a container?). Run this on the PC, or name one: " +
        "--host-culture en-ZA")]
    NoMachineCulture,

    [Description("a language without a region")]
    LanguageWithoutRegion,

    [Description("'{Name}' names no region ({Reason}). Use a culture such as en-ZA.")]
    [Holes(typeof(CultureNamesNoRegionHoles))]
    CultureNamesNoRegion,

    [Description("Culture: {Name} -> region {Region}, currency {Code} ({Symbol})")]
    [Holes(typeof(CultureFoundHoles))]
    CultureFound,

    [Description("Rate: 1 {Base} = {Rate} {Code} (as of {AsOf}, {Source})")]
    [Holes(typeof(RateFoundHoles))]
    RateFound,

    [Description("Costs are shown as charged, in {Base}.")]
    [Holes(typeof(ShownAsChargedHoles))]
    ShownAsCharged,

    [Description("No config/.env found: copy config/.env.example to config/.env first.")]
    NoEnvFile,

    [Description("Note: DISPLAY_CURRENCY={Code} is set and overrides it.")]
    [Holes(typeof(DisplayCurrencyOverridesHoles))]
    DisplayCurrencyOverrides,

    [Description("Restart the hosts to use it: the containers (docker compose ... up -d), and a New Chat in Rider.")]
    RestartHosts,

    [Description(
        "The PC's culture (F14): costs are shown in its region's currency. Written by usage-probe --host-culture.")]
    HostCultureComment,

    [Description("{Path}: {Count} requests; models {Models}; prices as of {AsOf}")]
    [Holes(typeof(LedgerSummaryHoles))]
    LedgerSummary,

    [Description(
        "{Rows} sub-session roll-up rows ({Requests} requests, {Cost}) listed, not totalled: the subagents' own " +
        "requests are rows in the ledger they ran on")]
    [Holes(typeof(LedgerRollUpsHoles))]
    LedgerRollUps,

    [Description("Per piece of work (a fix's task, a triage or a chat session)")]
    SectionPerWork,

    [Description("Per purpose")] SectionPerPurpose,

    [Description("Per run")] SectionPerRun,

    [Description("All")] SectionAll,

    [Description("Updated HOST_CULTURE={Name} in {File}")]
    [Holes(typeof(HostCultureUpdatedHoles))]
    HostCultureUpdated,

    [Description("Added HOST_CULTURE={Name} in {File}")]
    [Holes(typeof(HostCultureAddedHoles))]
    HostCultureAdded,
}
