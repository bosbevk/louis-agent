namespace LouisAgent.SessionStream.Pacing;

/// <summary>
/// How much of an answer shows, frame by frame (F154, Louis's pick A of three): the words come at a steady pace, not in
/// the bursts the model streams them in. Here, where the web and the engine both read it, so a page and a host pace
/// by one rule (F155). About 90 letters a second; faster when far behind what has
/// arrived, so a thousand letters behind is back within <see cref="FarBehind"/> in about a second and a half; and once
/// the answer is done, the last couple of hundred letters show in about a second and a quarter. It never shows more
/// than has arrived, and never takes back what it showed.
/// </summary>
/// <remarks>
/// Ask the type (ADR 0123): the rule is one pure function the caption asks each frame, tested with times given to it,
/// so it needs no clock and no browser. It keeps part letters, since 90 a second is a letter and a half a frame, and the
/// caption shows the whole ones (<see cref="Letters"/>).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public static class WordPace
{
    /// <summary>The steady pace, in letters a second: about fifteen words.</summary>
    public const double Steady = 90;

    /// <summary>How many letters behind what has arrived it may fall before it speeds up.</summary>
    public const int FarBehind = 240;

    /// <summary>How long, in seconds, it takes to close a gap once it has fallen far behind.</summary>
    public const double CatchUp = 1.5;

    /// <summary>How long, in seconds, it takes to show whatever is left once the answer is done.</summary>
    public const double Finish = 1.2;

    /// <summary>How much shows a frame later: this much shown now, this much arrived, this long since the last frame.</summary>
    public static double Next(double shown, int arrived, TimeSpan since, bool done)
    {
        double behind = arrived - shown;
        if (behind <= 0) return shown;

        // The steady pace, faster when far behind, faster still to finish once the answer is done. Each faster pace keeps a
        // floor that does not shrink with the gap, since a pace of the gap alone slows as it closes and never arrives.
        double pace = Steady;
        if (behind > FarBehind) pace = Math.Max(pace, (behind + FarBehind) / CatchUp);
        if (done) pace = Math.Max(pace, Steady + behind / Finish);

        return Math.Min(arrived, shown + pace * since.TotalSeconds);
    }

    /// <summary>The whole letters to draw of what <see cref="Next"/> says shows.</summary>
    public static int Letters(double shown) => (int)Math.Floor(shown);
}
