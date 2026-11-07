using LouisAgent.Accounts.Contracts;

namespace LouisAgent.Web.Accounts.Client;

/// <summary>
/// Who is signed in, as the app knows it: read once from the API, kept here, and announced when it changes, so the
/// menu, the pages and the auth state draw the same person. Null is a guest; whether the instance has accounts at
/// all is read with it.
/// </summary>
/// <remarks>
/// A scoped state with a <c>Changed</c> event, the way the chat's states work (the Razor rule: change travels through
/// a state's event, and a page never keeps its own copy).
/// </remarks>
public sealed class AccountState(AccountApi api)
{
    /// <summary>The signed-in person, or null for a guest.</summary>
    public AccountInfo? Me { get; private set; }

    /// <summary>Whether the API has accounts at all; false on an instance with no database.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Whether <see cref="LoadAsync"/> has run.</summary>
    public bool Loaded { get; private set; }

    public bool SignedIn => Me is not null;

    /// <summary>Fires after the person changed: signed in, signed out, or the profile edited.</summary>
    public event Action? Changed;

    /// <summary>Reads who is signed in, once; a later call reads again.</summary>
    public async Task LoadAsync()
    {
        Enabled = await api.EnabledAsync();
        Me = Enabled ? await api.MeAsync() : null;
        Loaded = true;
        Changed?.Invoke();
    }

    /// <summary>The person the API just answered with (a sign-in, a profile edit).</summary>
    public void Set(AccountInfo? me)
    {
        Me = me;
        Loaded = true;
        Changed?.Invoke();
    }

    /// <summary>Signed out, or the account deleted.</summary>
    public void Clear() => Set(null);
}
