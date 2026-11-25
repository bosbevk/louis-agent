using LouisAgent.Core.Pricing.Rates;

namespace LouisAgent.Orchestrator.Messages.Holes.Startup;

/// <summary>The currency every cost the orchestrator reports is shown in.</summary>
public sealed record CostsShownInHoles(DisplayCurrency Display);
