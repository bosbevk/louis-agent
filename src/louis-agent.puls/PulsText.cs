using LouisAgent.Web.Shared;

namespace LouisAgent.Puls;

/// <summary>Puls speaks Afrikaans: the shared chat components' words, in Afrikaans (the page's own are in ChatPage).</summary>
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
        RolePhases = new Dictionary<string, RolePhases> { ["puls-cook"] = new("Soek bestanddele", "Bou resep") },
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

        Currency = "Geldeenheid",
        AsCharged = "Soos gehef",
        ConvertedAt = "1 {0} = {1} {2} op {3}; die grootboek hou wat gehef is",
    };
}
