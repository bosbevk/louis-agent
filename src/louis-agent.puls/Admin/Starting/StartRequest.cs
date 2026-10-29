namespace LouisAgent.Puls.Admin.Starting;

/// <summary>
/// What a card's Run button, or a row's Replay button, asks the page for: run this check, this many times, playing a
/// recorded run back if one is named (FX28).
/// </summary>
/// <param name="Check">The check to run.</param>
/// <param name="Repeats">How many repeats the card's switch is on, or the recording had.</param>
/// <param name="Replay">The recorded run to play back, web and model; none for a run that asks the model.</param>
public sealed record StartRequest(string Check, int Repeats, string? Replay = null);
