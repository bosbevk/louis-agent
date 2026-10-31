using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Markdown;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Compare;

/// <summary>
/// The first turn's answer links to at least one of the hosts, or a subdomain of one (<c>shop.woolworths.co.za</c>
/// is Woolworths, <c>notcheckers.co.za</c> isn't Checkers): an answer that must send the reader to a source. A host
/// written in plain words is no link. A turn the infrastructure broke is Unknown; a turn that streamed an error fails.
/// </summary>
/// <remarks>
/// A Specification over the answer (Evans and Fowler), with the check contract's host rule,
/// <see cref="Links.IsHost"/>, the one a shop rule uses for a price's shop (Louis's pick, F49.3 step 1a: one rule, not
/// two copies). A link is any web address in the answer, inline, reference-style or bare, found by cutting the answer
/// at spaces and markdown's brackets and keeping what starts with a web scheme, never a pattern (ADR 0057). Its
/// <see cref="Entry"/> builds it from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public sealed class LinksToHostCheck(IReadOnlyList<string> hosts, bool required = true) : ICheck
{
    /// <summary>What ends a link in an answer: a space, or markdown's and HTML's brackets and quotes.</summary>
    private static readonly char[] LinkEnds = [' ', '\t', '\r', '\n', '(', ')', '[', ']', '<', '>', '"', '\''];

    /// <summary>The sentence's punctuation a link written bare can end on, which is no part of it.</summary>
    private static readonly char[] Punctuation = ['.', ',', ';', ':', '!', '?'];

    /// <inheritdoc />
    public string Name => "links-to-host";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Reads the first turn's links, after making sure that turn can be judged.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(GradeFirst(trial.Transcript.Turns.FirstOrDefault()));

    /// <summary>
    /// The grade of the first turn: Unknown with no turn or a broken one, a fail on a streamed error, then a pass
    /// naming the host it links to, or a fail naming the hosts.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade GradeFirst(TurnTranscript? first)
    {
        if (first is null) return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoTurn));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return unknown;
        if (EngineEvents.StreamedError(first))
            return Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TurnFailed));

        // A site as the file writes it, in any case and with spaces around it, is the same site.
        var linked = LinksIn(EngineEvents.AnswerOf(first));
        var host = hosts.FirstOrDefault(site => linked.Any(url => Links.IsHost(url, site.Trim().ToLowerInvariant())));
        return host is null
            ? Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.LinksToNoHost, new { Hosts = string.Join(", ", hosts) }))
            : Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.LinksToHostPass, new { Host = host }));
    }

    /// <summary>The web addresses in the answer, without the sentence's punctuation a bare one ends on.</summary>
    private static IReadOnlyList<string> LinksIn(string answer) =>
    [
        .. answer.Split(LinkEnds, StringSplitOptions.RemoveEmptyEntries)
            .Select(piece => piece.TrimEnd(Punctuation))
            .Where(piece => piece.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                            piece.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
    ];

    /// <summary>What a file can set: the hosts, any one of which will do, and whether the check must pass.</summary>
    public sealed record Settings(IReadOnlyList<string> Hosts, bool Required = true);

    /// <summary>The check as a file names it, <c>links-to-host</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("links-to-host", CheckKind.FixedRule)
    {
        /// <summary>
        /// Builds the check from its settings, refusing an empty list of hosts: no answer could pass.
        /// </summary>
        protected override ICheck Create(Settings settings, CheckContext context) => settings.Hosts.Count > 0
            ? new LinksToHostCheck(settings.Hosts, settings.Required)
            : throw Refused(CheckCatalogMessage.NothingListed, "hosts");
    }
}
