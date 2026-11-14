using LouisAgent.Diagrams.Engine.Components;
using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Tests.Components;

/// <summary>
/// CL65.6: what a part of a component diagram needs is asked in one place. A pin was built in four, each with
/// its width worked out beside it, and its height known to three more (ADR 0123).
/// </summary>
public class ComponentSizesTests
{
    /// <summary>
    /// A pin stands on an edge: half of it above the edge and half under, from the left it is told, as wide as
    /// its name needs with its arrow.
    /// </summary>
    [Test]
    public void APin_StandsHalfAboveItsEdge_AsWideAsItsName()
    {
        var pin = ComponentSizes.Pin(Geometries.InUse, on: "deck", name: "Items", up: true, left: 100, edge: 200);

        Assert.Multiple(() =>
        {
            Assert.That((pin.On, pin.Name, pin.Up), Is.EqualTo(("deck", "Items", true)));
            Assert.That(pin.Edge, Is.EqualTo(200));
            Assert.That(pin.Edge - pin.Box.Top, Is.EqualTo(pin.Box.Bottom - pin.Edge), "as much above as under");
            Assert.That((pin.Box.Left, pin.Box.Width), Is.EqualTo((100.0, ComponentSizes.PinWidth("Items"))));
        });
    }

    /// <summary>
    /// A chip after a name takes its own width and the gap before it, and no room at all when there is no chip.
    /// </summary>
    [Test]
    public void AChipAfterAName_TakesItsWidthAndAGap_AndNothingWithoutOne() =>
        Assert.Multiple(() =>
        {
            Assert.That(ComponentSizes.ChipAfterName("page"),
                Is.EqualTo(ComponentSizes.ChipGap + ComponentSizes.ChipWidth("page")));
            Assert.That(ComponentSizes.ChipAfterName(null), Is.Zero);
        });
}
