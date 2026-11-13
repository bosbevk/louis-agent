using LouisAgent.Web.Components.Editing.Inline;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Editing.Inline;

/// <summary>
/// F142: a page edited in place is drawn by components that each hold a part of one value. A part can be changed
/// exactly when someone listens for the whole to change, and its change is told as the whole with that part
/// changed. So a public page, where nobody listens, has nothing to click, with no "edit mode" passed around.
/// </summary>
public class PartChangesTests
{
    /// <summary>A whole of two parts.</summary>
    private sealed record Person(string Name, string Town);

    private static readonly Person Louis = new("Louis", "Malmesbury");

    /// <summary>A changed part is told to the whole's listener as the whole with that part changed.</summary>
    [Test]
    public async Task AChangedPart_IsToldAsTheChangedWhole()
    {
        Person? told = null;
        var whole = EventCallback.Factory.Create<Person>(this, changed => told = changed);

        var town = whole.Of<Person, string>(this, changed => Louis with { Town = changed });
        await town.InvokeAsync("Paternoster");

        Assert.Multiple(() =>
        {
            Assert.That(town.HasDelegate, Is.True);
            Assert.That(told, Is.EqualTo(new Person("Louis", "Paternoster")));
        });
    }

    /// <summary>With nobody listening for the whole, nobody listens for the part either.</summary>
    [Test]
    public void WithNobodyListeningForTheWhole_ThePartHasNoListener()
    {
        var nobody = default(EventCallback<Person>);

        var town = nobody.Of<Person, string>(this, changed => Louis with { Town = changed });

        Assert.That(town.HasDelegate, Is.False);
    }
}
