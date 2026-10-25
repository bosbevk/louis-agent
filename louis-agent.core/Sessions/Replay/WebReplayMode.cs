namespace LouisAgent.Core.Sessions.Replay;

/// <summary>
/// What a session does with the web input its tools read (F18 step 4a; ADR 0024: an enum, parsed once where it enters): nothing
/// (<see cref="Off"/>, the default: it reads the live web and saves nothing), save every visit (<see cref="Record"/>), or answer
/// only from what a recording saved (<see cref="Replay"/>), never opening the network.
/// </summary>
public enum WebReplayMode
{
    Off,
    Record,
    Replay,
}
