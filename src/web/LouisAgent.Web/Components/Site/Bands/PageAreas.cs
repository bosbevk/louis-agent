namespace LouisAgent.Web.Components.Site.Bands;

/// <summary>
/// The areas of the page in view (F118): every band and area mark puts itself here as it draws and takes itself off
/// as it goes, and the index puts them in the order their sections stand on the page, so the section index always
/// lists the page the visitor is looking at.
/// </summary>
/// <remarks>
/// One list on the frame's cascade, a Registry the parts fill (the same shape as a card deck and its cards, F113):
/// the frame draws the index from it and never knows a page's sections, so a new site gets the index for nothing.
/// Without it a site writes its index by hand, and the other pages have none, which is what F118 fixes.
/// </remarks>
public sealed class PageAreas
{
    private readonly List<IndexEntry> _items = [];

    /// <summary>The areas in page order.</summary>
    public IReadOnlyList<IndexEntry> Items => _items;

    /// <summary>Raised when an area comes or goes, so the index redraws.</summary>
    public event Action? Changed;

    /// <summary>Puts an area on the list, or renames it when the same id draws again with another label.</summary>
    public void Add(string id, string label)
    {
        int at = _items.FindIndex(entry => entry.Id == id);
        if (at >= 0 && _items[at].Label == label) return;
        if (at >= 0) _items[at] = new IndexEntry(id, label);
        else _items.Add(new IndexEntry(id, label));
        Changed?.Invoke();
    }

    /// <summary>Takes an area off the list when its band leaves the page.</summary>
    public void Remove(string id)
    {
        if (_items.RemoveAll(entry => entry.Id == id) > 0) Changed?.Invoke();
    }

    /// <summary>Puts the areas in the order the page has them; one the page doesn't name yet stays where it was, at the end.</summary>
    public void Reorder(string[] ids)
    {
        var sorted = _items.OrderBy(entry => Array.IndexOf(ids, entry.Id) is var at && at >= 0 ? at : ids.Length).ToList();
        if (sorted.SequenceEqual(_items)) return;
        _items.Clear();
        _items.AddRange(sorted);
        Changed?.Invoke();
    }
}
