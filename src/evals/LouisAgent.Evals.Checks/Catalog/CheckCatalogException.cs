namespace LouisAgent.Evals.Catalog;

/// <summary>
/// The catalogue can't be trusted, or was asked for a check type it doesn't have; the text says which.
/// </summary>
public sealed class CheckCatalogException(string message) : Exception(message);
