using LouisAgent.Puls.Text;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LouisAgent.Puls;
using LouisAgent.Web.Components.Areas;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// louis-agent.api serves this app, so the API is on the same origin.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton(PulsText.Afrikaans); // Puls speaks Afrikaans
builder.Services.AddSingleton(PulsAccountText.Afrikaans); // and so do its sign-in and profile pages (F61.2)
// The admin with the recipes' views, the accounts and the eval studio (F127.6), after Puls's own words so they stay.
builder.Services.AddSiteAreas(PulsAreas.Name);

await builder.Build().RunAsync();
