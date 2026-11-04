namespace LouisAgent.Puls.Content;

/// <summary>Puls's starters (F20), in South African context; the text the user finishes is the starter's end.</summary>
public static class Templates
{
    public static readonly IReadOnlyList<string> Sections = ["Uitgelig", "Gewild", "Lewenswenke"];
    public static readonly IReadOnlyList<string> Categories = ["Werk", "Huis", "Leer", "Geld", "Kos"];

    public static readonly IReadOnlyList<Template> All =
    [
        new("i-compass", "Reisbeplanner", "Beplan 'n naweek of vakansie: roete, verblyf en koste.", "Uitgelig", "Huis",
            "Beplan vir my 'n langnaweek in die Drakensberg vir twee mense: roete, verblyf en 'n begroting in rand. Ons vertrek uit "),
        new("i-chart", "Maandbegroting", "Sit jou inkomste en uitgawes in 'n duidelike begroting.", "Uitgelig", "Geld",
            "Help my om 'n maandbegroting op te stel. My netto inkomste is R"),
        new("i-file", "CV-hulp", "Skryf of verbeter jou CV vir 'n spesifieke pos.", "Uitgelig", "Werk",
            "Verbeter my CV vir hierdie pos. Die pos is: "),
        new("i-sparkle", "Braai-meester", "'n Braaimenu met hoeveelhede en 'n tydsplan.", "Gewild", "Kos",
            "Stel 'n braaimenu saam vir 10 mense, met hoeveelhede, 'n inkopielys en 'n tydsplan. Ons het "),
        new("i-pencil", "Brief in Afrikaans", "'n Formele of vriendelike brief, reg vir stuur.", "Gewild", "Werk",
            "Skryf vir my 'n formele brief in Afrikaans aan "),
        new("i-search", "Vergelyk en kies", "Vergelyk produkte of dienste, met voor- en nadele.", "Gewild", "Geld",
            "Vergelyk vir my die volgende opsies met voor- en nadele, en beveel een aan: "),
        new("i-clock", "Weekbeplanning", "Maak 'n realistiese plan vir jou week.", "Lewenswenke", "Huis",
            "Help my om my week te beplan. Hierdie week moet ek: "),
        new("i-home", "Huiswerk-helper", "Verduidelik 'n onderwerp stap vir stap, met voorbeelde.", "Lewenswenke", "Leer",
            "Verduidelik vir 'n graad 10-leerder, stap vir stap en met 'n voorbeeld: "),
        new("i-coin", "Belasting-vrae", "Verstaan jou belasting (SARS) in gewone taal.", "Lewenswenke", "Geld",
            "Verduidelik in gewone taal hoe dit werk met SARS: "),
    ];
}
