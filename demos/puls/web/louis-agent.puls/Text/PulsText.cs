using LouisAgent.Choices;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Text;

namespace LouisAgent.Puls.Text;

/// <summary>Puls speaks Afrikaans: the shared chat components' words, in Afrikaans.</summary>
public static class PulsText
{
    public static readonly ChatText Afrikaans = new()
    {
        Thinking = "Dink…",
        ThoughtFor = "Gedink vir {0}s",
        Input = "Invoer",
        Result = "Resultaat",
        Processing = "BESIG",
        // F34, Louis: "it must actually say soek bestanddele en dan bou resep".
        RolePhases = new Dictionary<string, RolePhases>
        { [PulsWebRole.Cook.Wire()] = new("Soek bestanddele", "Bou resep") },
        Stopped = "Gestop",

        DelegatedTo = "Oorgedra aan",
        MinionLabel = "Minion",
        OnModel = "op",
        FollowUp = "vervolg",
        Specialist = "spesialis",
        Task = "Taak",
        Working = "Besig",
        Work = "Werk",
        Done = "Klaar",
        Failed = "Misluk",
        Starting = "Begin",
        ToolCallOne = "gereedskapoproep",
        ToolCallMany = "gereedskapoproepe",
        ThoughtOne = "gedagte",
        ThoughtMany = "gedagtes",

        NewChat = "Nuwe gesprek",

        AddFavourite = "Merk as gunsteling",
        RemoveFavourite = "Haal uit gunstelinge",
        Share = "Deel",
        Rename = "Hernoem",
        Archive = "Argiveer",
        Unarchive = "Haal uit argief",
        Delete = "Vee uit",
        NewName = "Nuwe naam",
        MoreFor = "Meer vir {0}",

        SearchChats = "Soek gesprekke",
        SearchPlaceholder = "Soek gesprekke…",
        NoChatsFor = "Geen gesprekke vir \"{0}\" nie.",
        Recent = "Onlangs",
        Chats = "Gesprekke",
        Archived = "Geargiveer",
        SearchChoose = "kies",
        SearchOpen = "maak oop",

        HideSidebar = "Versteek die kantbalk",
        CloseMenu = "Maak die kieslys toe",
        Favourites = "Gunstelinge",
        Projects = "Projekte",
        NewProject = "Nuwe projek",
        Settings = "Instellings",
        ApiKey = "API-sleutel",
        ApiKeyHint = "net as AGENT_API_KEY gestel is",
        Location = "Ligging",
        LocationExample = "bv. Pretoria",
        Locating = "Soek…",
        UseMyLocation = "Gebruik my ligging",
        Names = "Name",
        ShowMotion = "Wys animasies al vra my stelsel minder",
        SystemAsksNoLess = " (jou stelsel vra nie minder nie)",

        ShowSidebar = "Wys die kantbalk",
        MoreForThisChat = "Meer vir hierdie gesprek",
        ToggleWork = "Wys of versteek die werkpaneel",
        ShowWork = "Wys die werkpaneel",

        Credits = "Krediete",
        CreditsTitle = "Krediete en gebruik: {0} hierdie maand",
        CreditsHeading = "Krediete · betaal soos jy gebruik",
        ThisChat = "Hierdie gesprek",
        ThisMonth = "Hierdie maand",
        ChatOne = "gesprek",
        ChatMany = "gesprekke",
        CreditsNote = "Geen intekening nie: jy betaal net vir die vrae wat jy vra, spesialiste ingesluit. Volgens die gesprekke in hierdie blaaier.",
        BuyCredits = "Koop krediete",

        Language = "Taal",

        Profile = "Profiel",
        GuestInitial = "G",
        Guest = "Gas",
        LocalOnly = "Plaaslik in hierdie blaaier",
        Theme = "Tema",
        ThemeDark = "Donker",
        ThemeLight = "Lig",
        ThemeLightSoon = "Die ligte tema",
        ThemeAuto = "Outomaties (stelsel)",
        ThemeAutoSoon = "Outomatiese tema",
        SignOut = "Teken uit",

        SessionGone =
            "Hierdie gesprek se sessie bestaan nie meer op die bediener nie (die API het herbegin). Begin 'n nuwe gesprek om voort te gaan.",
        NeedsKey = "Die API het 'n sleutel nodig: maak Instellings oop en vul AGENT_API_KEY in.",
        StillAnswering = "Hierdie gesprek antwoord nog; wag daarvoor of druk Stop.",
        ApiUnreachable = "Kan nie Puls se API bereik nie. Loop die puls-houer?",

        Currency = "Geldeenheid",
        AsCharged = "Soos gehef",
        ConvertedAt = "1 {0} = {1} {2} op {3}; die grootboek hou wat gehef is",
    };
}
