using LouisAgent.Composition.Words;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Site.Content.WorkWithUs.Parts;
using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Site.Content.WorkWithUs;

/// <summary>
/// What the Work with us page says (F177), slide by slide: what we build, where to see it working, the seven steps
/// of a piece of work, how it is paid, what the client holds at the end, and what running AI costs.
/// </summary>
/// <remarks>
/// The page's lists as data, so the page only configures the framework's tiles and steps (ADR 0125). Each list is a
/// dictionary factory keyed by an enum of its parts (ADR 0132), as the trust page's facts are.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0132-words-come-from-a-dictionary-factory.md">ADR 0132: words come from a dictionary factory</seealso>
public static class WorkWithUsWords
{
    /// <summary>Half of the price, as a tile's figure.</summary>
    private const string Half = "50%";

    /// <summary>The kinds of work we take on.</summary>
    public static readonly PartWords<BuildKind, WorkTile> WhatWeBuild =
        PartWords<BuildKind, WorkTile>.Of(kind => kind switch
        {
            BuildKind.Assistant => new(DiagramIcon.Message, "An assistant that knows your business",
                "It answers your customers from your own documents, prices and policies, day and night."),
            BuildKind.Documents => new(DiagramIcon.File, "Document upload and analysis",
                "People upload a file. The AI reads it and explains what matters in plain words."),
            BuildKind.InsideYourApp => new(DiagramIcon.Settings, "AI inside the app you have",
                "We add AI features to your website or system, without starting again."),
            BuildKind.Prototype => new(DiagramIcon.Browser, "A complete prototype",
                "Sign-up, uploads, AI answers and a dashboard, ready to test with real users."),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });

    /// <summary>Where a visitor can try what we built.</summary>
    public static readonly PartWords<ProofPlace, WorkTile> SeeItWorking =
        PartWords<ProofPlace, WorkTile>.Of(place => place switch
        {
            ProofPlace.Guide => new(DiagramIcon.Message, "Orbi, this site's agent",
                "It sits in the corner of every page. Ask it about our work, and it answers."),
            ProofPlace.Demos => new(DiagramIcon.Play, "Three demo businesses",
                "A guesthouse, a restaurant and a law practice, each with its own agent. The businesses are made up.",
                "/demos", "Watch one work"),
            ProofPlace.Framework => new(DiagramIcon.Stack, "The framework",
                "Our own engine runs every agent we build. Its site shows how it works and how it is tested.",
                "https://framework.louis-agent.dev", "Read about the framework"),
            _ => throw new ArgumentOutOfRangeException(nameof(place))
        });

    /// <summary>The steps of a piece of work, in their order.</summary>
    public static readonly PartWords<WorkStep, Step> HowWeWork =
        PartWords<WorkStep, Step>.Of(step => step switch
        {
            WorkStep.DiscoveryCall => new("Discovery call.", "A video call about your idea and your users."),
            WorkStep.WrittenSpec => new("Written spec.", "What it will do, in plain words, for you to check."),
            WorkStep.FixedQuote => new("Fixed quote.", "A proposal with one price and a timeline."),
            WorkStep.Build => new("Build.", "We build it, and show you as it takes shape."),
            WorkStep.TestAndRefine => new("Test and refine.", "You try it, and we fix what isn't right."),
            WorkStep.GoLive => new("Go live.", "It goes out to your customers."),
            WorkStep.Handover => new("Handover.", "A guide and a walkthrough, so it is yours to run."),
            _ => throw new ArgumentOutOfRangeException(nameof(step))
        });

    /// <summary>The two halves the price is paid in.</summary>
    public static readonly PartWords<PaymentHalf, PaymentShare> HowYouPay =
        PartWords<PaymentHalf, PaymentShare>.Of(half => half switch
        {
            PaymentHalf.AfterTesting => new("The first half", Half, "after you have tested it, before it goes live"),
            PaymentHalf.AfterGoLive => new("The second half", Half, "after it is live with your customers"),
            _ => throw new ArgumentOutOfRangeException(nameof(half))
        });

    /// <summary>What the client holds when the work is handed over.</summary>
    public static readonly PartWords<HandoverItem, WorkTile> WhatYouGet =
        PartWords<HandoverItem, WorkTile>.Of(item => item switch
        {
            HandoverItem.WorkingApp => new(DiagramIcon.Play, "The working app",
                "Live, and used by your customers or your team."),
            HandoverItem.Code => new(DiagramIcon.Code, "The code",
                "Tested and readable, so another developer can carry it on."),
            HandoverItem.WrittenGuide => new(DiagramIcon.Book, "A written guide",
                "How it works, and how to change what you will want to change."),
            HandoverItem.Walkthrough => new(DiagramIcon.Users, "A walkthrough",
                "A call where we show you and your team every part."),
            _ => throw new ArgumentOutOfRangeException(nameof(item))
        });

    /// <summary>What running AI costs, and how the client sees it.</summary>
    public static readonly PartWords<RunningCost, WorkTile> RunningCosts =
        PartWords<RunningCost, WorkTile>.Of(cost => cost switch
        {
            RunningCost.PaidPerUse => new(DiagramIcon.Clock, "Paid for each time it is used",
                "Every question the AI answers costs a small amount, paid to the AI provider. More use costs more, " +
                "and a quiet month costs less."),
            RunningCost.Measured => new(DiagramIcon.Chart, "Measured, so you can see it",
                "Our framework records what every AI request cost. You see what you spend and never have to guess."),
            _ => throw new ArgumentOutOfRangeException(nameof(cost))
        });
}
