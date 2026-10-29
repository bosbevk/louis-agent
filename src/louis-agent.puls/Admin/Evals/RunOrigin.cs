namespace LouisAgent.Puls.Admin.Evals;

/// <summary>
/// Where a run's results came from: the model answered live, a recording was played back, or old transcripts were
/// graded again. Only a recorded run measures the model anew.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum RunOrigin
{
    /// <summary>The model answered live.</summary>
    Recorded,

    /// <summary>A recording's answers played back, for free.</summary>
    Replay,

    /// <summary>A stored run's transcripts graded again with today's graders, for free.</summary>
    Rescore
}
