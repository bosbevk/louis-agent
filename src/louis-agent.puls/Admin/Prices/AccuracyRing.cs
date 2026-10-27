namespace LouisAgent.Puls.Admin.Prices;

/// <summary>
/// One mode's accuracy donut (FX24): its shopping-list rows as right (priced with their source, or rightly without a price where the town has no shop),
/// estimated, guessed (a price with no source) and with no price. The cook gives estimates, so they are a slice of their own and count as priced.
/// </summary>
public sealed record AccuracyRing(int Right, int Estimated, int Guessed, int NoPrice, string Share, string Over)
{
    public static AccuracyRing Of(int priced, int noShop, int guessed, int? estimated, int items)
    {
        int right = priced + noShop, estimates = estimated ?? 0;
        int noPrice = Math.Max(0, items - right - estimates - guessed);
        return new AccuracyRing(right, estimates, guessed, noPrice, $"{(double)(right + estimates) / items:0%}",
            $"{right + estimates} / {items}");
    }
}
