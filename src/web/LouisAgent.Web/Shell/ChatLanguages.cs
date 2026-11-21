using LouisAgent.Web.Api;
using LouisAgent.Api.Contracts.Info;
using LouisAgent.Web.Scripts;

namespace LouisAgent.Web.Shell;

/// <summary>
/// The languages a new chat can speak: those the API has a voice for under the site's prefix. This browser's choice
/// wins if the API still offers it, then the site's default. A running chat keeps the voice it opened with.
/// </summary>
/// <remarks>
/// Presentation Model (Fowler): the choice and its rules live here, and the menu only draws them. The site names its
/// own voices and default, so the framework knows no site's.
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaDev/PresentationModel.html">Presentation Model (Fowler)</seealso>
public sealed class ChatLanguages(AgentApi api, JsModule preferences, string voicePrefix, string defaultLanguage)
{
    private readonly string _default = defaultLanguage;

    /// <summary>The languages on offer.</summary>
    public IReadOnlyList<string> Available { get; private set; } = [defaultLanguage];

    /// <summary>The language a new chat speaks.</summary>
    public string Current { get; private set; } = defaultLanguage;

    /// <summary>The voice a new chat opens with.</summary>
    public string Role => voicePrefix + Current;

    /// <summary>Reads what the API offers and what this browser chose.</summary>
    public async Task LoadAsync()
    {
        try
        {
            var roles = await api.GetAsync<RolesReply>("v1/roles", InfoJson.SnakeCase);
            var codes = roles.Agent.Where(role => role.StartsWith(voicePrefix, StringComparison.Ordinal))
                .Select(role => role[voicePrefix.Length..]).ToList();
            if (codes.Count > 0) Available = codes;
        }
        catch (Exception)
        {
            // An API that can't say: the site's default is all there is.
        }

        string? saved = await preferences.InvokeAsync<string?>("loadLanguage");
        Current = FirstOffered(saved, _default) ?? Available[0];
    }

    /// <summary>Keeps the language, and this browser remembers it.</summary>
    public async Task ChooseAsync(string code)
    {
        Current = code;
        await preferences.InvokeVoidAsync("saveLanguage", code);
    }

    /// <summary>The first of these the API still offers, if any.</summary>
    private string? FirstOffered(params string?[] codes) =>
        codes.FirstOrDefault(code => code is not null && Available.Contains(code));
}
