namespace LouisAgent.Api.Evals.Store;

/// <summary>
/// A short, fresh id for something the admin keeps: twelve hex characters, enough that two never meet and short
/// enough to read in a web address. A run gets its id here, and so does a draft.
/// </summary>
/// <remarks>
/// Extract Function and Move Function (Fowler): the run's private <c>NewId</c> moved here so a draft's id is made the
/// same way, from one place (Louis's pick).
/// </remarks>
public static class ShortId
{
    /// <summary>A new id.</summary>
    public static string New() => Guid.NewGuid().ToString("N")[..12];
}
