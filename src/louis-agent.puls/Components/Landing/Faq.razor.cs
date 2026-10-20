using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Landing;

public partial class Faq
{
    private static readonly (string Question, string Answer)[] Questions =
    [
        ("Wat is Puls?", "'n KI-assistent wat jou vraag na 'n spesialis stuur: 'n tweede agent met net die gereedskap wat die taak nodig het. Jy sien die werk terwyl dit gebeur."),
        ("Watter model gebruik Puls?", "'n Claude-model van Anthropic, soos op die bediener ingestel. Die koste van elke vraag word gewys."),
        ("Wat kos dit?", "Jy betaal soos jy gebruik: elke vraag se koste word gewys, spesialiste ingesluit. Geen intekening nie. Krediete kom binnekort."),
        ("Is Puls oopbron?", "Nee. Puls is gebou op louis-agent, en die kode is op GitHub te lees, maar alle regte is voorbehou: jy mag dit nie sonder skriftelike toestemming gebruik, kopieer, verander of deel nie."),
        ("Waar word my gesprekke gestoor?", "In jou blaaier. Die bediener hou 'n gesprek net solank dit loop; ná 'n herbegin begin jy 'n nuwe een."),
        ("Kan ek dit in Engels gebruik?", "Ja: vra in Engels en Puls antwoord in Engels. Die skerms is in Afrikaans."),
    ];
}
