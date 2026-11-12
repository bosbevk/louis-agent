using LouisAgent.Starter;
using LouisAgent.Web.Components.Areas;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// louis-agent.api serves this app, so the API is on the same origin.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
// The chat, the admin, the accounts and the eval studio (F127.6); a site fills in its own words and views there.
builder.Services.AddSiteAreas(StarterAreas.Name);

await builder.Build().RunAsync();
