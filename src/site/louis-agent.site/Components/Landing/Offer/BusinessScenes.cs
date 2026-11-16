using LouisAgent.Site.Components.Showroom.Windows;
using LouisAgent.Web.Components.Site.Product;
using LouisAgent.Web.Components.Site.Product.Page;
using LouisAgent.Web.Components.Site.Product.Scene;

namespace LouisAgent.Site.Components.Landing.Offer;

/// <summary>
/// The three made-up businesses as the landing's product window plays them, one after another: each its own page,
/// the question its visitor asked and the answer its agent gave, then the request it drew and sent on a yes.
/// </summary>
/// <remarks>
/// The question, the answer and the place shown are each business's recorded talk, <see cref="SiteWindowTalks"/>, so
/// the window says what the demo says; the cards are each business's own page. The rest of the scene, the request and
/// its yes, is the design page's.
/// </remarks>
public static class BusinessScenes
{
    /// <summary>The scenes, in the order the window plays them.</summary>
    public static IReadOnlyList<ProductScene> All { get; } = [Guesthouse(), Restaurant(), LawPractice()];

    /// <summary>The guesthouse: a weekend with the dog, the garden room opened, the booking sent.</summary>
    private static ProductScene Guesthouse()
    {
        var talk = SiteWindowTalks.Seemeeu;
        IReadOnlyList<ProductRoom> rooms =
        [
            new("images/showroom/seemeeu-sea-view.webp", "The sea-view doubles", "R1,450 a night"),
            new("images/showroom/seemeeu-family.webp", "The family room", "R1,850 a night"),
            new("images/showroom/seemeeu-garden.webp", "The garden room", "R1,250 a night. Dogs welcome.", true)
        ];
        IReadOnlyList<SlipRow> rows =
        [
            new("Nights", "Friday and Saturday"), new("Guests", "Two, and a dog"),
            new("Reach you at", "marike@example.com")
        ];
        var slip = new ProductSlip("BOOKING REQUEST", "The garden room", rows, "Status", "Not sent yet");
        var steps = Steps("seemeeu.example", talk, "Then the garden room, please. Marike, marike@example.com",
            "Booking request", "owner");
        return new ProductScene("seemeeu.example", "Seemeeu Guesthouse", ["HOME", "ROOMS", "STAY"], rooms, steps,
            "0 calls", "Seemeeu · ask us")
        { Slip = slip };
    }

    /// <summary>The restaurant: Sunday lunch for five, the children's menu opened, the table sent.</summary>
    private static ProductScene Restaurant()
    {
        var talk = SiteWindowTalks.Wheatfield;
        IReadOnlyList<ProductRoom> dishes =
        [
            new("images/showroom/wheatfield-roast.webp", "The Sunday roast", "12:00 to 15:00"),
            new("images/showroom/wheatfield-pie.webp", "The children's menu", "R75 a dish", true),
            new("images/showroom/wheatfield-malva.webp", "Malva pudding", "For after")
        ];
        IReadOnlyList<SlipRow> rows =
        [
            new("When", "Sunday at 12:30"), new("Guests", "Five, two of them small"),
            new("Reach you at", "anna@example.com")
        ];
        var slip = new ProductSlip("TABLE REQUEST", "Sunday lunch", rows, "Status", "Not sent yet");
        var steps = Steps("wheatfieldkitchen.example", talk,
            "Then a table for five at 12:30, please. Anna, anna@example.com", "Table request", "kitchen");
        return new ProductScene("wheatfieldkitchen.example", "Wheatfield Kitchen", ["HOME", "MENU", "VISIT"], dishes,
            steps, "0 calls", "Wheatfield · ask us")
        { Slip = slip };
    }

    /// <summary>
    /// The law practice: a question it may not answer, the page that explains it, a consultation sent.
    /// </summary>
    private static ProductScene LawPractice()
    {
        var talk = SiteWindowTalks.Koringland;
        IReadOnlyList<ProductRoom> cards =
        [
            new("images/showroom/koringland-papers.webp", "What we do", "Four areas of law."),
            new("images/showroom/koringland-consultation.webp", "A first consultation", "Thirty minutes."),
            new("images/showroom/koringland-desk.webp", "Wills and estates", "A will, and an estate after a death.",
                true)
        ];
        IReadOnlyList<SlipRow> rows =
        [
            new("About", "A will after a divorce"), new("How", "At the office or by video call"),
            new("Reach you at", "pieter@example.com")
        ];
        var slip = new ProductSlip("CONSULTATION REQUEST", "A first consultation", rows, "Status", "Not sent yet");
        var steps = Steps("koringland.example", talk, "Then I would like a consultation. Pieter, pieter@example.com",
            "Consultation request", "practice");
        return new ProductScene("koringland.example", "Koringland Attorneys", ["HOME", "WHAT WE DO", "CONSULTATIONS"],
            cards, steps, "0 calls", "Koringland · ask us")
        { Slip = slip };
    }

    /// <summary>
    /// One business's talk as the design page plays it: the recorded question, the place opened, the recorded
    /// answer, then the visitor's yes to a request the agent draws, offers and sends once.
    /// </summary>
    private static IReadOnlyList<SceneStep> Steps(string address, WindowTalk talk, string follow, string card,
        string to)
    {
        var shown = new SceneStep(SceneStepKind.Call, $"ShowPage {talk.Shown}")
        {
            Stage = SceneStage.Shown,
            Address = address + talk.Shown,
            Foot = "1 call"
        };
        var drawn = new SceneStep(SceneStepKind.Call, $"ShowCard {card}") { Stage = SceneStage.Card, Foot = "2 calls" };
        var sent = new SceneStep(SceneStepKind.Call, "SendLead, once")
        {
            Stage = SceneStage.Sent,
            Foot = "3 calls · 2.1 s · US$0.005, an example",
            Status = $"Sent to the {to}"
        };
        return
        [
            new(SceneStepKind.Ask, talk.Question), new(SceneStepKind.Think, "Thinking", 1200), shown,
            new(SceneStepKind.Answer, talk.Answer), new(SceneStepKind.Wait, Ms: 1500),
            new(SceneStepKind.Ask, follow), new(SceneStepKind.Think, "Thinking", 1000), drawn,
            new(SceneStepKind.Answer, $"May I send it to the {to} now?"),
            new(SceneStepKind.Offer, "Book it|Change something|Not now"), new(SceneStepKind.Wait, Ms: 1700),
            new(SceneStepKind.Press), new(SceneStepKind.Think, "Thinking", 700), sent
        ];
    }
}
