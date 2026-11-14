using LouisAgent.Diagrams.Components.Spec;
using LouisAgent.Diagrams.Engine.Components.Placed;
using LouisAgent.Diagrams.Engine.Components.Placed.Lines;

namespace LouisAgent.Diagrams.Engine.Components;

/// <summary>
/// Places what stands around the root of a component diagram (F134.7): a column before it and a column after it,
/// each neighbour level with the box it talks to, and the lines between a neighbour and its box.
/// </summary>
/// <remarks>
/// Every line runs straight across, so nothing has to be routed: a neighbour is put level with its box, or under
/// the neighbour above it when that place is taken, and its lines end at the box when they are level with it and
/// at the root's own edge when they are not.
/// </remarks>
internal static class NeighbourLayout
{
    /// <summary>No neighbour is narrower than this.</summary>
    private const double Narrowest = 190;

    /// <summary>
    /// From its top edge to under its name, a row for its chip, its line of code, a line of its note, a row for a
    /// thing given.
    /// </summary>
    private const double Header = 44, ChipRow = 24, CodeLine = 20, NoteLine = 16, Row = 24;

    /// <summary>
    /// The room at its foot, between two neighbours in a column, how near a box's corner a line may end, and the
    /// room a heading takes over the neighbour it opens a group with.
    /// </summary>
    private const double Foot = 14, Between = 18, Corner = 8, HeadingRoom = 14;

    /// <summary>How far under a neighbour's top its first line runs, and its second.</summary>
    private const double FirstLine = 30, SecondLine = 54;

    /// <summary>
    /// How wide one letter is taken to be: in its name, which is code, in its note, and in a thing given.
    /// </summary>
    private const double NameChar = 7.6, NoteChar = 5.9, RowChar = 6.1;

    /// <summary>How many letters of a neighbour's note fit a line.</summary>
    private const int NoteWrap = 30;

    /// <summary>
    /// How wide the column on one side is: as wide as its widest neighbour. Nothing for no neighbours.
    /// </summary>
    public static double ColumnWidth(IReadOnlyList<ComponentNeighbour> around, NeighbourSide side) =>
        around.Where(neighbour => neighbour.Side == side).Select(Width).DefaultIfEmpty(0).Max();

    /// <summary>
    /// Every neighbour at its place: what comes before the root in a column at the left, as wide as it is told,
    /// and what comes after in a column that starts where it is told. In a column each is level with its box, or
    /// under the one above.
    /// </summary>
    public static IReadOnlyList<PlacedNeighbour> Place(IReadOnlyList<ComponentNeighbour> around,
        IReadOnlyList<PlacedBox> boxes, double before, double after) =>
    [
        .. Column(around, boxes, NeighbourSide.Before, 0, before),
        .. Column(around, boxes, NeighbourSide.After, after, ColumnWidth(around, NeighbourSide.After))
    ];

    /// <summary>The lines between a neighbour and the box it talks to, each straight across.</summary>
    public static IEnumerable<PlacedLine> Lines(PlacedNeighbour placed, IReadOnlyList<PlacedBox> boxes)
    {
        var neighbour = placed.Neighbour;
        var box = BoxOf(neighbour, boxes);
        double near = neighbour.Side == NeighbourSide.Before ? placed.Right : placed.X;

        // Each thing it gives is a row of its own, with its own line to the box, in the neighbour's colour.
        foreach (var (given, at) in (neighbour.Gives ?? []).Select((given, at) => (given, at)))
        {
            double y = RowY(placed, at);
            yield return new PlacedLine(LineKind.Gives, [new(near, y), new(Far(y), y)], given, neighbour.Tone);
        }

        double first = placed.Y + FirstLine, second = placed.Y + SecondLine;
        if (neighbour.Reads) yield return new PlacedLine(LineKind.Reads, [new(near, first), new(Far(first), first)]);
        if (neighbour.Calls is { } calls)
            yield return new PlacedLine(LineKind.Calls, [new(Far(first), first), new(near, first)], calls);
        if (neighbour.Back is { } back)
        {
            double y = neighbour.Calls is null ? first : second;
            yield return new PlacedLine(LineKind.Back, [new(near, y), new(Far(y), y)], back);
        }

        // Where a level line meets the picture: the box's own edge when it is level with the box, else the root's.
        double Far(double y)
        {
            var met = y > box.Y + Corner && y < box.Bottom - Corner ? box : boxes[0];
            return neighbour.Side == NeighbourSide.Before ? met.X : met.Right;
        }
    }

    /// <summary>The baseline of a neighbour's line of code, under its name and its chip.</summary>
    public static double CodeY(PlacedNeighbour placed) => Under(placed) + 8;

    /// <summary>
    /// The baseline of a line of a neighbour's note, under its name, its chip and its line of code. Each next line
    /// is a note's line lower.
    /// </summary>
    public static double NoteY(PlacedNeighbour placed, int line) =>
        Under(placed) + (placed.Neighbour.Code is null ? 0 : CodeLine) + 8 + line * NoteLine;

    /// <summary>The middle, top to bottom, of the row a neighbour gives its thing number so-many on.</summary>
    public static double RowY(PlacedNeighbour placed, int at) =>
        Under(placed) + (placed.Neighbour.Code is null ? 0 : CodeLine) + placed.NoteLines.Count * NoteLine +
        at * Row + Row / 2;

    /// <summary>Where a neighbour's own rows start: under its name, and under its chip when it has one.</summary>
    private static double Under(PlacedNeighbour placed) =>
        placed.Y + Header + (placed.Neighbour.Kind is null ? 0 : ChipRow);

    /// <summary>
    /// One side's column: its neighbours from the top down, in the order of the boxes they talk to. One that opens
    /// a group stands a little lower, for its heading.
    /// </summary>
    private static IEnumerable<PlacedNeighbour> Column(IReadOnlyList<ComponentNeighbour> around,
        IReadOnlyList<PlacedBox> boxes, NeighbourSide side, double x, double width)
    {
        double free = 0;
        foreach (var neighbour in around.Where(one => one.Side == side).OrderBy(one => BoxOf(one, boxes).Y))
        {
            double y = Math.Max(BoxOf(neighbour, boxes).Y, free) + (neighbour.Heading is null ? 0 : HeadingRoom);
            double height = Height(neighbour);
            yield return new PlacedNeighbour(neighbour, x, y, width, height, NoteLines(neighbour));
            free = y + height + Between;
        }
    }

    /// <summary>The box a neighbour talks to: the one it names, else the root.</summary>
    private static PlacedBox BoxOf(ComponentNeighbour neighbour, IReadOnlyList<PlacedBox> boxes) =>
        boxes.FirstOrDefault(box => box.Box.Id == neighbour.To) ?? boxes[0];

    /// <summary>A neighbour's note, a line at a time.</summary>
    private static IReadOnlyList<string> NoteLines(ComponentNeighbour neighbour) =>
        neighbour.Note is null ? [] : TextWrap.Lines(neighbour.Note, NoteWrap);

    /// <summary>
    /// How wide a neighbour must be: for its name, its chip, its line of code, its note and the things it gives.
    /// </summary>
    private static double Width(ComponentNeighbour neighbour)
    {
        double name = 22 + neighbour.Name.Length * NameChar + 20;
        double chip = neighbour.Kind is null ? 0 : 22 + ComponentSizes.ChipWidth(neighbour.Kind) + 20;
        double code = neighbour.Code is null ? 0 : 22 + neighbour.Code.Length * RowChar + 20;
        double note = NoteLines(neighbour).Select(line => 22 + line.Length * NoteChar + 20).DefaultIfEmpty(0).Max();
        double rows = (neighbour.Gives ?? []).Select(given => 22 + given.Length * RowChar + 34).DefaultIfEmpty(0).Max();

        return new[] { Narrowest, name, chip, code, note, rows }.Max();
    }

    /// <summary>
    /// How tall a neighbour is: its name, its chip, its line of code, its note and a row for each thing it gives,
    /// and never shorter than the two lines a call and its answer need.
    /// </summary>
    private static double Height(ComponentNeighbour neighbour)
    {
        double own = Header + (neighbour.Kind is null ? 0 : ChipRow) + (neighbour.Code is null ? 0 : CodeLine) +
                     NoteLines(neighbour).Count * NoteLine + (neighbour.Gives?.Count ?? 0) * Row + Foot;
        return Math.Max(own, SecondLine + Foot + 6);
    }
}
