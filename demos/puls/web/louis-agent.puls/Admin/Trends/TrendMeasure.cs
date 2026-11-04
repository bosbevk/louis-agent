using LouisAgent.Api.Contracts.Admin.Trends;

namespace LouisAgent.Puls.Admin.Trends;

/// <summary>
/// One panel of the trend: its measure's word (which picks its hover note), its title, how to read the value from a
/// run's point, how to write it, and a fixed top for a share (100%) or none to scale to the highest value.
/// </summary>
public sealed record TrendMeasure(
    string Topic,
    string Title,
    Func<TrendPointRow, double?> Value,
    Func<double, string> Label,
    double? FixedTop = null);
