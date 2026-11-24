namespace LouisAgent.Site.Content.Trust.Parts;

/// <summary>The facts of what the agent may do and what stops it, in the order the trust page tells them.</summary>
public enum AiSafetyFact
{
    OnlyTheToolsItNeeds,
    APersonApproves,
    ModelTextIsData,
    HardLimits,
    EveryRequestMeasured,
    EvalsReplayed,
    InjectionTestsAndAuditLog
}
