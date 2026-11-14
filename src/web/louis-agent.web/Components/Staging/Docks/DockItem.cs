using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Web.Components.Staging.Docks;

/// <summary>
/// One button of a dock (F134.1): what the dock needs to draw it, and nothing of what it stands for. A page turns
/// each of its own things into one of these.
/// </summary>
/// <remarks>
/// A presentation model (Fowler): the dock draws plain data and stays free of what the page's things are (Louis's
/// pick of three: a record per button, over a generic dock with a function per fact, or one with a template). So any
/// project fills a dock with five facts per button.
/// </remarks>
/// <param name="Id">
/// Its id on the page, which is also the last part of its address, such as <c>restaurant-demo</c>.
/// </param>
/// <param name="Name">Its name, shown beside its icon when it is the current one and read out for every one.</param>
/// <param name="Icon">Its icon.</param>
/// <param name="Accent">Its colour; with none, the colour of the place the dock stands in.</param>
/// <param name="Group">
/// The group it belongs to. A divider is drawn where the group changes. With none, there are no groups.
/// </param>
/// <seealso href="https://martinfowler.com/eaaDev/PresentationModel.html">Presentation Model (Fowler)</seealso>
public sealed record DockItem(
    string Id,
    string Name,
    DiagramIcon Icon,
    TileAccent Accent = TileAccent.None,
    string? Group = null);
