namespace LouisAgent.Web.Components.Chat.Shell;

/// <summary>
/// The parts of the chat's frame a site can turn off. Every one is on by default, so a site only names what doesn't
/// suit it: a coding agent has no use for a place, a language menu or buying credits.
/// </summary>
/// <remarks>
/// Hook methods from Template Method, as one record: the frame stays one frame, and a part left out takes its gap with
/// it. A slot for each part would make every site repeat the parts it keeps.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/template-method">Template Method (Refactoring.Guru)</seealso>
/// <param name="Credits">The menu with this chat's and this month's cost.</param>
/// <param name="Language">The menu choosing the language a new chat speaks.</param>
/// <param name="Location">The place the person gives, in the settings.</param>
/// <param name="Projects">The projects in the sidebar.</param>
/// <param name="Unfinished">What only says it's coming: the voice button, feedback, other models and so on.</param>
public sealed record ChatFeatures(
    bool Credits = true,
    bool Language = true,
    bool Location = true,
    bool Projects = true,
    bool Unfinished = true);
