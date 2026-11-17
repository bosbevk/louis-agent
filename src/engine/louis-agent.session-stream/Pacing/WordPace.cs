namespace LouisAgent.SessionStream.Pacing;

/// <summary>
/// How much of an answer shows, frame by frame (F154, Louis's pick A of three): the words come at a steady pace, not in
/// the bursts the model streams them in. Here, where the web and the engine both read it, so a page and a host pace
/// by one rule (F155). About 90 letters a second while little is waiting; with more waiting, fast enough to show it
/// within the catch-up window, so the pace grows with the backlog and never steps: a thousand letters behind is within
/// 240 in about a second and a half, and 240 waiting all show in about two. Once the answer is done, the last couple
/// of hundred letters show in about a second and a quarter. It never shows more than has arrived, and never takes back
/// what it showed.
/// </summary>
/// <remarks>
/// Ask the type (ADR 0123): the rule is one pure function its caller asks each frame, tested with times given to it,
/// so it needs no clock and no browser. It keeps part letters, since 90 a second is a letter and a half a frame, and the
/// caller shows the whole ones (<see cref="CountLetters"/>). Continuous, the steady pace or the backlog over a short
/// window, as smoothing a streamed answer is commonly done, since a fixed speed either falls behind a fast model or
/// shows its bursts again (F155, Louis: "web should change to also continuous").
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public static class WordPace
{
    /// <summary>The steady pace, in letters a second: about fifteen words.</summary>
    public const double Steady = 90;

    /// <summary>
    /// The catch-up window, in seconds: behind, it goes at least as fast as would show what is waiting within it, so the
    /// pace grows with the backlog and never steps (F155).
    /// </summary>
    public const double CatchUp = 1.0;

    /// <summary>How long, in seconds, it takes to show whatever is left once the answer is done.</summary>
    public const double Finish = 1.2;

    /// <summary>How much shows a frame later: this much shown now, this much arrived, this long since the last frame.</summary>
    public static double Advance(double shown, int arrived, TimeSpan since, bool done)
    {
        double behind = arrived - shown;
        if (behind <= 0) return shown;

        // The steady pace, or what is waiting over the catch-up window when that is faster, so the pace grows with the
        // backlog and never steps; faster still to finish once the answer is done. The steady pace is the floor, since a
        // pace of the gap alone slows as it closes and never arrives.
        double pace = Math.Max(Steady, behind / CatchUp);
        if (done) pace = Math.Max(pace, Steady + behind / Finish);

        return Math.Min(arrived, shown + pace * since.TotalSeconds);
    }

    /// <summary>The whole letters to draw of what <see cref="Advance"/> says shows.</summary>
    public static int CountLetters(double shown) => (int)Math.Floor(shown);
}
