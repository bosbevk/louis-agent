using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using LouisAgent.Packs.Leads.Sending;
using LouisAgent.Packs.Leads.Tools;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Packs.Leads;

/// <summary>
/// The guide's way to tell the owner someone is interested (F112): one toolset, <c>leads</c>, with one tool that sends
/// a lead. Off without <c>LEADS_TO</c>; without <c>RESEND_API_KEY</c> the lead goes to the log and the model is told
/// so.
/// </summary>
/// <remarks>
/// The same shape as every pack (ADR 0027): a list of toolsets and the files the model reads, naming no identity and no
/// other pack. The sender is picked here by the options (Strategy): Resend with a key, the log without one. A tool
/// and not a prompt rule (ADR 0080): the guide sends the lead instead of telling the visitor to go and write a mail.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
public sealed class LeadsPack(LeadsOptions options, ILoggerFactory? loggers = null, HttpClient? http = null)
    : IAgentPack
{
    /// <inheritdoc />
    public string Name => LeadsToolset.Leads.Wire();

    /// <inheritdoc />
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(LeadsToolset.Leads.Wire(),
            "Tell the owner someone wants a website with an agent or a custom agent",
            GuideFile: PackToolset.Guide(LeadsToolset.Leads.Wire()),
            MissingSettings: _ => options.To is null ? [LeadsOptions.ToSetting] : [],
            ToolTypes: [typeof(LeadTools)],
            CreateTools: _ => [new LeadTools(Sender(options, loggers, http), TimeProvider.System)]),
    ];

    /// <inheritdoc />
    public IFileProvider Prompts { get; } = EmbeddedPrompts.Of(typeof(LeadsPack).Assembly);

    /// <inheritdoc />
    public IReadOnlyList<string> Requires => [];

    /// <inheritdoc />
    public bool ComesBeforeTheEngine => true;

    /// <summary>Resend with a key, the log without one.</summary>
    internal static ILeadSender Sender(LeadsOptions options, ILoggerFactory? loggers, HttpClient? http) =>
        options.ApiKey is null
            ? new LogLeadSender((loggers ?? NullLoggerFactory.Instance).CreateLogger<LogLeadSender>())
            : new ResendLeadSender(http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) }, options);
}
