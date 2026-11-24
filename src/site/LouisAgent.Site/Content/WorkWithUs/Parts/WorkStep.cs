namespace LouisAgent.Site.Content.WorkWithUs.Parts;

/// <summary>The steps of a piece of work, from the first call to the handover, in their order.</summary>
public enum WorkStep
{
    DiscoveryCall,
    WrittenSpec,
    FixedQuote,
    Build,
    TestAndRefine,
    GoLive,
    Handover
}
