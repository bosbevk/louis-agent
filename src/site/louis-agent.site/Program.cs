using LouisAgent.Site;
using LouisAgent.Web.Components.Areas;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The site's host serves this app, so the API is on the same origin.
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// The showroom with its demos, the guide in the corner on the chat's plumbing (F127.6), and every place's address.
builder.Services.AddSiteAreas(LouisAgentDevAreas.Name);

await builder.Build().RunAsync();
