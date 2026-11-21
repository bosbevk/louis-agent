namespace LouisAgent.Core.Config;

/// <summary>How long a prompt-cache entry lives after its last use (F4). Provider-neutral: a caching strategy maps it.</summary>
public enum CacheLifetime
{
    /// <summary>5 minutes: cheaper to write (1.25× on Anthropic); right when requests come less than 5 minutes apart.</summary>
    FiveMinutes,

    /// <summary>1 hour: dearer to write (2×); pays off when requests sharing the entry come 5–60 minutes apart.</summary>
    OneHour,
}
