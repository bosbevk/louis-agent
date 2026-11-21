namespace LouisAgent.Packs.ShopPrices.Estimates;

/// <summary>How a line's estimate was made, so the answer can say what it rests on.</summary>
public enum EstimateKind
{
    /// <summary>The table prices this very pack size.</summary>
    SamePack,

    /// <summary>Scaled from the nearest pack the table prices (or its price per kilogram or litre).</summary>
    Scaled,

    /// <summary>The line gives no size: one of the table's packs, the first it prices.</summary>
    TablePack,

    /// <summary>The table prices the product only in another kind of unit (by the item, not by weight).</summary>
    OtherUnit,

    /// <summary>No product in the tables fits the line, or none of its packs has a price.</summary>
    NotFound,
}
