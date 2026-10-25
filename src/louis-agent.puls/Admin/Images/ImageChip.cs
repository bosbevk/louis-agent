using LouisAgent.Puls.Admin.Prices;

namespace LouisAgent.Puls.Admin.Images;

/// <summary>The chip that says whether a cook answer showed a photo of the dish (FX24); a scores document from before the count has none to show.</summary>
public static class ImageChip
{
    public static PricedChip? Of(int? images) => images switch
    {
        null => null,
        > 0 => new PricedChip("image shown", "ok"),
        _ => new PricedChip("no image", "warn"),
    };
}
