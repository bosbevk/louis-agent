namespace LouisAgent.Core.Tools.Web.Search;

/// <summary>
/// How recent a search's results must be (F32 step 1b; ADR 0024): parsed once from <c>WebSearch</c>'s
/// <c>freshness</c>, with the first letter as an alias (<c>d</c>, <c>w</c>, <c>m</c>, <c>y</c>); each provider writes it
/// in its own words.
/// </summary>
public enum SearchRecency
{
    Day,
    Week,
    Month,
    Year,
}
