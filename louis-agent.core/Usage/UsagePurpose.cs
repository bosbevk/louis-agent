namespace LouisAgent.Core.Usage;

/// <summary>Why a request was made. Strings, so the ledger stays readable as JSON.</summary>
public static class UsagePurpose
{
    public const string Turn = "turn";
    public const string Continue = "continue";
    public const string Summary = "summary";
    public const string Compaction = "compaction";
    public const string Triage = "triage";
}
