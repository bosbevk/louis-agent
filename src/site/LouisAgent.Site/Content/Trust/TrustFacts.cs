using LouisAgent.Composition.Words;
using LouisAgent.Site.Content.Trust.Parts;
using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Site.Content.Trust;

/// <summary>
/// What the trust page says (F165), band by band: where the site runs, what it keeps, what its agent may do, what
/// happens if something breaks, and where a reviewer's checklist for paying customers stands. Each fact says whether
/// it is in place, and each one in place names the file that shows it, so the page can only say what the code does.
/// </summary>
/// <remarks>
/// The page's facts as data, so the page only configures the framework's tiles (ADR 0125), as the framework's
/// comparison page holds its rows. Each band is a dictionary factory keyed by an enum of its facts (ADR 0132), as
/// Build your own's steps are (Louis's pick B of three, F126.1: the lists themselves are the factories, over keeping
/// plain lists the factory filled, or both side by side).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0132-words-come-from-a-dictionary-factory.md">ADR 0132: words come from a dictionary factory</seealso>
public static class TrustFacts
{
    /// <summary>The Azure architecture, which several facts rest on.</summary>
    private const string Architecture = "src/docs/architecture/deployment-in-azure.md";

    /// <summary>The site's own app in Azure, its settings and what it mounts.</summary>
    private const string SiteApp = "src/site/deploy/azure/louis-agent-app.yaml";

    /// <summary>
    /// The how-to of the sites in Azure, which names what runs there: the storage, the vault, the registry. A fact
    /// rests on a file the framework keeps, never a demo's (FX215).
    /// </summary>
    private const string AzureHowTo = "src/docs/guides/deploy-the-sites.md";

    /// <summary>The image the site runs from.</summary>
    private const string Dockerfile = "src/site/docker/Dockerfile";

    /// <summary>The privacy page, which holds the detail of what the site keeps.</summary>
    private const string Privacy = "src/site/LouisAgent.Site/Pages/About/PrivacyPage.razor";

    /// <summary>Where it runs, and who can reach it.</summary>
    public static readonly PartWords<InfrastructureFact, TrustFact> WhereItRuns =
        PartWords<InfrastructureFact, TrustFact>.Of(fact => fact switch
        {
            InfrastructureFact.InSouthAfrica => new(TileStatus.Done, "In Azure, in South Africa",
                "The site is an Azure Container App in the South Africa North region. It sleeps when nobody visits " +
                "and wakes on the next visit.", SiteApp),
            InfrastructureFact.OnlyHttps => new(TileStatus.Done, "Only https, only the site",
                "The internet reaches the site on port 443 with a managed certificate, and plain http is refused. " +
                "Nothing else we run in Azure has a public address.", Architecture),
            InfrastructureFact.Encrypted => new(TileStatus.Done, "Encrypted on the way and at rest",
                "Every visit is https. The site's logs sit in Azure Storage, which Microsoft encrypts at rest with " +
                "256-bit AES, and that can't be switched off.", AzureHowTo),
            InfrastructureFact.NoPasswordsInTheCode => new(TileStatus.Done, "No passwords in the code",
                "Every key lives in Azure Key Vault. The site reads it by reference with its own managed identity, " +
                "and a release signs in to Azure from GitHub without a stored password.",
                "src/docs/adr/0097-puls-deploys-from-github-without-a-password.md"),
            InfrastructureFact.OneThingPerContainer => new(TileStatus.Done, "A container that runs one thing",
                "The site runs in its own container from its own image. It runs no shell and builds nothing. It " +
                "reads its docs and passes on a visitor's details.", Dockerfile),
            InfrastructureFact.OutboundNarrowed => new(TileStatus.Open, "Outbound traffic narrowed",
                "What the site may call out to is Azure's default. No rule of ours narrows it yet.", Architecture),
            InfrastructureFact.NotRoot => new(TileStatus.Open, "A container that isn't root",
                "The site's image doesn't name a user of its own yet, so it runs as the image's default user.",
                Dockerfile),
            _ => throw new ArgumentOutOfRangeException(nameof(fact), fact, null)
        });

    /// <summary>What we keep, who sees it, and the visitor's rights.</summary>
    public static readonly PartWords<DataProtectionFact, TrustFact> YourData =
        PartWords<DataProtectionFact, TrustFact>.Of(fact => fact switch
        {
            DataProtectionFact.WordsNotStored => new(TileStatus.Done, "Your words aren't stored",
                "The site holds a chat in memory while it runs and forgets it when it restarts. " +
                "For each answer it writes down the cost, the page and the tools used, never your words.", Privacy),
            DataProtectionFact.DetailsOnYourYes => new(TileStatus.Done, "Details sent only on your yes",
                "The agent shows what it will send and sends it once, when you press Send it. Nothing reaches us " +
                "before.", "src/docs/adr/0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md"),
            DataProtectionFact.AskCorrectDelete => new(TileStatus.Done, "Ask, correct, delete",
                "You may ask what we hold about you, have it corrected or deleted, and complain to the Information " +
                "Regulator.", Privacy),
            DataProtectionFact.CompaniesNamed => new(TileStatus.Done, "Every company that sees data, named",
                "The table below names each company that handles data for this site, what for, and where.", SiteApp),
            DataProtectionFact.RetentionPeriod => new(TileStatus.Open, "A written retention period",
                "The logs and the record of costs stay on the site's file share. There is no deletion schedule yet.",
                SiteApp),
            _ => throw new ArgumentOutOfRangeException(nameof(fact), fact, null)
        });

    /// <summary>What the agent may do, and what stops it.</summary>
    public static readonly PartWords<AiSafetyFact, TrustFact> AiSafety =
        PartWords<AiSafetyFact, TrustFact>.Of(fact => fact switch
        {
            AiSafetyFact.OnlyTheToolsItNeeds => new(TileStatus.Done, "Only the tools its job needs",
                "An agent's tools are fixed when a chat opens. The site's agent gets its docs, the site, and a way " +
                "to pass on your details, and a visitor can't ask for more.",
                "src/docs/adr/0006-toolsets-per-session.md"),
            AiSafetyFact.APersonApproves => new(TileStatus.Done, "A person approves what lasts",
                "Your details go out only on your yes. A skill an agent writes for later waits until a person " +
                "approves it.", "src/docs/adr/0033-what-the-agent-writes-has-a-scope.md"),
            AiSafetyFact.ModelTextIsData => new(TileStatus.Done, "What the model writes is data, never a command",
                "Nothing the model writes reaches a shell. A fetch refuses private addresses and obeys robots.txt, " +
                "and secret files are never read.", "src/docs/adr/0004-arguments-as-data.md"),
            AiSafetyFact.HardLimits => new(TileStatus.Done, "Hard limits on every answer",
                "At most 10 rounds of tool calls for each message. A result that is too big is summarised, and a " +
                "call cut off midway never runs.", "src/engine/LouisAgent.Core/Agent/Turns/ChatPipeline.cs"),
            AiSafetyFact.EveryRequestMeasured => new(TileStatus.Done, "Every request measured and watched",
                "Each request to the model is priced in a ledger, and each step of a chat is traced in Azure's " +
                "monitoring. One address opens at most 20 chats a day.", "src/docs/adr/0007-measure-every-request.md"),
            AiSafetyFact.EvalsReplayed => new(TileStatus.Done, "Evals that replay on every change",
                "The guide, the framework's Orbi and the triage team each have an eval of their conversations. Each " +
                "one replays from its recording on every pull request, for free, so a change that breaks one fails " +
                "the build.", "src/docs/adr/0134-an-eval-is-one-file-of-conversations.md"),
            AiSafetyFact.InjectionTestsAndAuditLog => new(TileStatus.Open, "Prompt-injection tests and an audit log",
                "A suite of injection attacks, checks on what the agent says, and a log of every action are " +
                "planned, not built.", "src/docs/plans/phase-7-checked-and-guarded.md"),
            _ => throw new ArgumentOutOfRangeException(nameof(fact), fact, null)
        });

    /// <summary>How a change reaches the site, how it would be rebuilt, and what is the business's to take.</summary>
    public static readonly PartWords<ContinuityFact, TrustFact> IfSomethingBreaks =
        PartWords<ContinuityFact, TrustFact>.Of(fact => fact switch
        {
            ContinuityFact.TestedThenReleased => new(TileStatus.Done, "Tested, then released",
                "A merge runs the tests, then releases the site as a new revision from an image with its own tag. " +
                "Earlier images stay in the registry.", ".github/workflows/deploy-sites.yml"),
            ContinuityFact.RebuiltFromCode => new(TileStatus.Done, "Rebuilt from code",
                "Everything the site stands on in Azure, its network, environment, registry, vault and storage, is " +
                "described in one file of code. Rebuilding it from nothing is that file, then the script that " +
                "releases the site.", "src/site/deploy/azure/main.bicep"),
            ContinuityFact.YourSiteIsYours => new(TileStatus.Done, "Your site is yours",
                "A site is its own project. Its words, brand, pages and data sit apart from the framework, in code " +
                "that can be handed over.", "src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md"),
            ContinuityFact.Backups => new(TileStatus.Open, "Backups",
                "The file share that holds the logs and the site's saved content has no backup set up. Chats " +
                "aren't stored, so no visitor's words can be lost.", AzureHowTo),
            ContinuityFact.Uptime => new(TileStatus.Open, "A promise of uptime",
                "The site runs in one region, and a sleeping site takes a moment to wake on the first visit. There " +
                "is no uptime figure yet.", Architecture),
            ContinuityFact.RecoveryRunbook => new(TileStatus.Open, "A tested recovery runbook",
                "The pieces to rebuild everything exist. A written, timed walk-through of a full recovery doesn't " +
                "yet.", "src/docs/guides/deploy-the-sites.md"),
            _ => throw new ArgumentOutOfRangeException(nameof(fact), fact, null)
        });

    /// <summary>A reviewer's checklist for a business before it pays, and where each item stands.</summary>
    public static readonly PartWords<ChecklistFact, TrustFact> BeforePayingCustomers =
        PartWords<ChecklistFact, TrustFact>.Of(fact => fact switch
        {
            ChecklistFact.OperatorAgreement => new(TileStatus.Open, "An operator agreement under POPIA",
                "A written agreement for when we process your customers' data on your behalf."),
            ChecklistFact.TenantIsolation => new(TileStatus.Open, "Tested tenant isolation",
                "Every account already carries its business, but no test yet proves one business can't reach " +
                "another's data.", "src/docs/adr/0088-a-tenant-on-every-account.md"),
            ChecklistFact.Encryption => new(TileStatus.Done, "Encryption on the way and at rest",
                "Every visit is https, and Azure encrypts what the site stores. Where it runs, above, has the detail.",
                AzureHowTo),
            ChecklistFact.BackupsAndRecovery => new(TileStatus.Open, "Documented backups and recovery",
                "Not set up yet. If something breaks, above, says why."),
            ChecklistFact.RetentionAndDeletion => new(TileStatus.Open, "Retention and deletion rules",
                "Chats aren't kept. The logs and the record of costs have no written period yet."),
            ChecklistFact.OthersNamed => new(TileStatus.Done, "The model's maker and others named",
                "Anthropic's model writes the answers. Every company that handles data is named under your data, " +
                "above.", SiteApp),
            ChecklistFact.IncidentProcedure => new(TileStatus.Open, "An incident and breach procedure",
                "A written plan for what we do, and whom we tell, when something goes wrong. POPIA asks that the " +
                "Information Regulator and the people affected are told."),
            ChecklistFact.TermsAndLiability => new(TileStatus.Open, "Terms, a service level and liability",
                "Customer terms, a service level and who answers for what are not written yet."),
            ChecklistFact.PenetrationTest => new(TileStatus.Open, "An independent penetration test",
                "No outside tester has attacked the site yet."),
            _ => throw new ArgumentOutOfRangeException(nameof(fact), fact, null)
        });

    /// <summary>Every fact on the page, in the page's order.</summary>
    public static readonly IReadOnlyList<TrustFact> All =
    [
        .. WhereItRuns.InOrder, .. YourData.InOrder, .. AiSafety.InOrder, .. IfSomethingBreaks.InOrder,
        .. BeforePayingCustomers.InOrder
    ];
}
