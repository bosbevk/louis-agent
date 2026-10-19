namespace LouisAgent.Puls.Content;

/// <summary>A ready-made starter (F20): a card on Verken and in Studio that opens a chat with its starter in the message box.</summary>
/// <param name="Icon">An icon class (i-*) from app.css.</param>
/// <param name="Section">Verken's section: Uitgelig, Gewild or Lewenswenke.</param>
/// <param name="Category">Studio's filter: Werk, Huis, Leer, Geld or Kos.</param>
public sealed record Template(string Icon, string Title, string Description, string Section, string Category, string Starter);
