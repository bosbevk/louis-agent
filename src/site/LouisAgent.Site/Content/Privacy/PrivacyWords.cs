using LouisAgent.Composition.Words;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Site.Content.Privacy.Parts;

namespace LouisAgent.Site.Content.Privacy;

/// <summary>
/// What the privacy page says the site keeps and sends (F145.1), tile by tile, in the order a visitor meets it. Every
/// line says what the code does today: a chat is kept for 30 days after its last message, a photo or a PDF never
/// (F43.1).
/// </summary>
/// <remarks>
/// The page's tiles as data, so the page only configures the framework's tiles (ADR 0125), keyed by an enum of its
/// parts (ADR 0132), as the trust page's facts are.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0132-words-come-from-a-dictionary-factory.md">ADR 0132: words come from a dictionary factory</seealso>
public static class PrivacyWords
{
    /// <summary>What the site keeps and sends of what a visitor does.</summary>
    public static readonly PartWords<PrivacyPart, PrivacyTile> WhatIsKept =
        PartWords<PrivacyPart, PrivacyTile>.Of(part => part switch
        {
            PrivacyPart.WhatYouType => new(DiagramIcon.User, "What you type",
                "Your messages go to Anthropic, the company whose model writes the answers, in the United States. " +
                "They are used to answer you. The site keeps your chat in Microsoft Azure, so it carries on after a " +
                "restart, and deletes it 30 days after your last message. Photos and PDFs you send are never kept."),
            PrivacyPart.YourDetails => new(DiagramIcon.Box, "Your details",
                "If you ask to be contacted, the agent asks for your name and how to reach you. It shows you what it " +
                "will send, and sends it only when you press Send it. It reaches us by email, through Resend, a mail " +
                "service."),
            PrivacyPart.YourAddress => new(DiagramIcon.Shield, "Your internet address",
                "It counts how many chats you open, up to 20 a day. The count is kept in memory for a day, and never " +
                "written down."),
            PrivacyPart.WhatIsWrittenDown => new(DiagramIcon.Database, "What is written down",
                "For each answer, the site writes down what it cost, the page you were on and which tools the agent " +
                "used. The timing of each step goes to Azure's monitoring. Neither holds your words."),
            PrivacyPart.WhereItRuns => new(DiagramIcon.Server, "Where it runs",
                "In Azure's South African region. Two things leave the country. Your messages go to the model, to " +
                "answer you, and the mail with your details goes through Resend."),
            PrivacyPart.YourLook => new(DiagramIcon.Settings, "Your look",
                "The look you pick, light or dark, is kept in your own browser. It is sent nowhere."),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        });
}
