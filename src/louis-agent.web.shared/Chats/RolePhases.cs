namespace LouisAgent.Web.Shared.Chats;

/// <summary>
/// What a specialist's role is doing, in an app's words (F34): <see cref="Gathering"/> while it searches, until it has
/// results and is writing, then <see cref="Building"/>. Puls's cook: "Soek bestanddele", then "Bou resep".
/// </summary>
public sealed record RolePhases(string Gathering, string Building);
