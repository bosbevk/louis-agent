namespace LouisAgent.Site.Content.Trust.Parts;

/// <summary>A reviewer's checklist for a business before it pays, in the order the trust page tells it.</summary>
public enum ChecklistFact
{
    OperatorAgreement,
    TenantIsolation,
    Encryption,
    BackupsAndRecovery,
    RetentionAndDeletion,
    OthersNamed,
    IncidentProcedure,
    TermsAndLiability,
    PenetrationTest
}
