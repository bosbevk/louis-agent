using LouisAgent.Site;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Site.Addresses;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The site's host serves this app, so the API is on the same origin.
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Every place on the site's pages has its own address (ADR 0107), the landing's under /home, read from the pages'
// routes.
builder.Services.AddSingleton(SiteAddresses.From("home", typeof(App).Assembly));

// The showroom with its demos, and the guide in the corner on the chat's plumbing (F127.6).
builder.Services.AddSiteAreas(LouisAgentDevAreas.Name);

await builder.Build().RunAsync();
