using LouisAgent.SiteFramework;
using LouisAgent.Web.Components.Areas;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The site's host serves this app, so the API is on the same origin.
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Every place on the site's pages has its own address (ADR 0107), the Overview's under /overview, read from the
// pages' routes.
builder.Services.AddSingleton(FrameworkAreas.Addresses());
builder.Services.AddSiteAreas(FrameworkAreas.Name);

await builder.Build().RunAsync();
