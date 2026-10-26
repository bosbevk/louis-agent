namespace LouisAgent.Puls.Admin.Runs;

/// <summary>
/// Where the run explorer stands after another repeat of the same run is chosen (2026-10-26): the conversation (or turn) that is open stays open when that repeat has it
/// too, a request goes up to its turn (the repeat's requests are other ones), and anything else is the overview. The conversations are the new repeat's, across the modes.
/// </summary>
public static class RepeatSwitch
{
    public static int LevelAfter(int level, string? task, int? turn, IEnumerable<(string Task, IEnumerable<int> Turns)> conversations)
    {
        bool stays = task is not null && conversations.Any(c => c.Task == task && (turn is null || c.Turns.Contains(turn.Value)));
        return stays ? Math.Min(level, 3) : 0;
    }
}
