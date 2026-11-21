namespace LouisAgent.Web.Components.Admin;

/// <summary>
/// Whether the API refused the admin key, as the cards on the admin's home found (F144): a card that is refused says
/// so here and stays quiet, and the home asks for the key once, rather than once per card.
/// </summary>
/// <remarks>
/// Observer (Gang of Four), as <c>ReviewQueueState</c> is: a service the cards and the home share; the cards report,
/// the home listens, and neither knows the other. Louis's pick of three, over the home asking the API itself (it would
/// borrow an area's address) and a key box always shown (it never says the key was wrong).
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/observer">Observer (Refactoring.Guru)</seealso>
public sealed class AdminAccess
{
    /// <summary>Why the key was refused, or none while it works.</summary>
    public string? Error { get; private set; }

    /// <summary>Raised when the key is refused or accepted again, so the home draws again.</summary>
    public event Action? Changed;

    /// <summary>A card was refused for this reason.</summary>
    public void Refuse(string error)
    {
        if (Error == error) return;
        Error = error;
        Changed?.Invoke();
    }

    /// <summary>A new key was saved: the cards read again, and refuse again if it is wrong too.</summary>
    public void Clear()
    {
        if (Error is null) return;
        Error = null;
        Changed?.Invoke();
    }
}
