namespace LouisAgent.Web.Evals.Client;

/// <summary>
/// A flip's body as the eval routes read it, <c>{"note": …}</c>. The routes' own record lives in the host, which a
/// browser project can't reference, so this is its tested twin (the client's tests read the body it writes).
/// </summary>
/// <param name="Note">Why the person flipped the mark, in one line, or none.</param>
public sealed record FlipBody(string? Note);
