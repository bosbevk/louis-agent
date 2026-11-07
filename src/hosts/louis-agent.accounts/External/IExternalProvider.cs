using Microsoft.AspNetCore.Authentication;

namespace LouisAgent.Accounts.External;

/// <summary>
/// One way to sign in through someone else: which, what its button says, whether this instance has its keys, and
/// how it joins the authentication pipeline.
/// </summary>
/// <remarks>
/// Strategy (Gang of Four), as the page renderers and the search providers are: each provider knows for itself
/// whether it's set up and how to build itself, so "is it on?" and "add it" can't drift apart, and a new provider
/// is one new class, never another case in two switches.
/// </remarks>
public interface IExternalProvider
{
    ExternalProvider Name { get; }

    /// <summary>What the button says: "Google", "Microsoft", "LinkedIn".</summary>
    string Label { get; }

    /// <summary>Whether the instance has the provider's keys; without them it isn't added and no button shows.</summary>
    bool IsConfigured { get; }

    /// <summary>Adds the provider's handler, under its wire name as the scheme, signing into Identity's external cookie.</summary>
    void AddTo(AuthenticationBuilder auth);
}
