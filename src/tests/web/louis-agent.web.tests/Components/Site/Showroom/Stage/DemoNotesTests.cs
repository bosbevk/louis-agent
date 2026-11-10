using LouisAgent.Web.Components.Site.Showroom.Stage;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Stage;

/// <summary>
/// F127.1 (Louis: "we must make notes on the demo screen for instance we could integrate with the booking system if
/// they have one etc. Custom Agent"): under a demo, what a custom agent could add for a business like it, a kicker, a
/// heading for the guide's tour and a card a note.
/// </summary>
public class DemoNotesTests
{
    /// <summary>The notes carry their kicker, their heading and each note's title and words, in order.</summary>
    [Test]
    public void TheNotes_HaveTheirKicker_TheirHeading_AndEachNote()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<DemoNotes>(parameters => parameters
            .Add(notes => notes.Kicker, "Make it yours")
            .Add(notes => notes.Heading, "A custom agent for your bakery could also")
            .Add(notes => notes.Notes,
            [
                new DemoNote("Take the orders", "It books the bread into your order book."),
                new DemoNote("Answer on WhatsApp", "The same agent, on WhatsApp too."),
            ]));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".notes-kicker").TextContent, Is.EqualTo("Make it yours"));
            Assert.That(cut.Find("h2.notes-heading").TextContent,
                Is.EqualTo("A custom agent for your bakery could also"));
            Assert.That(cut.FindAll("li.note .note-title").Select(title => title.TextContent),
                Is.EqualTo(new[] { "Take the orders", "Answer on WhatsApp" }));
            Assert.That(cut.Find("li.note .note-text").TextContent,
                Is.EqualTo("It books the bread into your order book."));
        });
    }
}
