using System.Net;
using System.Text;
using LouisAgent.Api.Contracts.Admin.Imports;
using LouisAgent.Testing;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Imports;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Imports;

/// <summary>
/// F55: an upload's card takes a file for each slot, posts them to the preview route, shows the report's facts, and
/// offers Save only after a clean preview that changes something.
/// </summary>
public class ImportCardTests
{
    /// <summary>The upload: two files.</summary>
    private static readonly ImportRow Upload = new("prices", "Prices from a release.",
        [new ImportSlotRow("all-urban", "all urban areas"), new ImportSlotRow("provinces", "provinces")]);

    /// <summary>What the preview route answers: two new months.</summary>
    private const string Preview = """
        {"period":"June 2026 to July 2026","changes":2,"saved":false,
         "facts":[{"label":"New months","value":"June 2026, July 2026"}],"problems":[]}
        """;

    /// <summary>Both files chosen, Preview shows the facts and then offers Save.</summary>
    [Test]
    public void APreview_ShowsTheFacts_ThenOffersSave()
    {
        using var ctx = new BunitContext();
        var posted = new List<string>();
        var cut = Render(ctx, posted);

        Assert.That(cut.Find(".import-card button").HasAttribute("disabled"), Is.True, "no files yet");
        foreach (var input in cut.FindComponents<InputFile>())
            input.UploadFiles(InputFileContent.CreateFromBinary([1, 2, 3], "release.xlsx"));
        cut.Find(".import-card button").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".import-facts").TextContent,
            Does.Contain("New months").And.Contain("June 2026, July 2026")), TimeSpan.FromSeconds(10));
        Assert.That(posted, Is.EqualTo(new[] { "/v1/admin/imports/prices/preview" }));
        Assert.That(cut.FindAll(".import-card button").Select(b => b.TextContent.Trim()),
            Is.EqualTo(new[] { "Preview", "Save" }));
    }

    /// <summary>The card over an API that answers the preview.</summary>
    private static IRenderedComponent<ImportCard> Render(BunitContext ctx, List<string> posted)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new StubHandler(request =>
        {
            posted.Add(request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Preview, Encoding.UTF8, "application/json")
            };
        });

        var settings = new ChatSettings();
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            settings));
        return ctx.Render<ImportCard>(p => p.Add(c => c.Form, new ImportForm(Upload)));
    }
}
