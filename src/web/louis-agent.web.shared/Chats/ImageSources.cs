namespace LouisAgent.Web.Shared.Chats;

/// <summary>
/// The images a turn may show (FX22): the union of what its blocks' tool results gave and what every sub-turn's did, recursively (a minion inside a
/// specialist too). Per turn, not per chat: a later turn's answer can't reuse an image an earlier turn read.
/// </summary>
public static class ImageSources
{
    public static IReadOnlySet<string> Of(Turn turn)
    {
        var images = new HashSet<string>(StringComparer.Ordinal);
        Collect(turn, images);
        return images;
    }

    private static void Collect(Turn turn, HashSet<string> images)
    {
        foreach (var block in turn.Blocks)
        {
            images.UnionWith(block.Images);
            if (block.SubTurn is { } sub) Collect(sub, images);
        }
    }
}
