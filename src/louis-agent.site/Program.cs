using LouisAgent.Site.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();

var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>();
app.MapGet("/health", Health.Ok);

app.Run();

/// <summary>The site's host, named so the tests can start it in memory.</summary>
public partial class Program;

/// <summary>What a container's health check asks for.</summary>
internal static class Health
{
    /// <summary>Says the site is up.</summary>
    public static IResult Ok() => TypedResults.Ok();
}
