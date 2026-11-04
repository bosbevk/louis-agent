using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Puls.Admin.Rates;

/// <summary>
/// Each mode's rates from the run's mode rows: a standard record, not an eval's score key. The runner wrote them on
/// every repeat's line, over the repeats so far, so a mode's are the last repeat's that has them; a mode from before
/// the rates has none.
/// </summary>
public static class ModeRatesReader
{
    /// <summary>One line per mode, in the order the modes first came.</summary>
    public static IReadOnlyList<ModeRate> Read(IReadOnlyList<ModeRow> modes) =>
    [
        .. modes.GroupBy(m => m.Name).Select(mode =>
            mode.Where(m => m.K is not null).OrderBy(m => m.Repeat).LastOrDefault() is { } last
                ? new ModeRate(mode.Key, last.K, last.PassAtK, last.PassHatK, last.Infrastructure ?? 0,
                    last.Retried ?? 0)
                : new ModeRate(mode.Key, null, null, null, 0, 0))
    ];
}
