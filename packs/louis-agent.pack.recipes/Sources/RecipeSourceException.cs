namespace LouisAgent.Packs.Recipes.Sources;

/// <summary>A search a site couldn't answer, with a message the cook can act on.</summary>
public sealed class RecipeSourceException(string message) : Exception(message);
