using LouisAgent.Site;
using LouisAgent.Site.Components.Showroom;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The site's host serves this app, so the API is on the same origin.
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// The guide in the corner: the API client its chat runs on, and the words and numbers it shows.
builder.Services.AddSingleton<ChatSettings>();
builder.Services.AddSingleton<ChatStore>();
builder.Services.AddScoped<AgentApi>();
builder.Services.AddScoped<CurrencyState>();
builder.Services.AddSingleton(new ChatText()); // the shared components' words, in English

// Every place on the site's pages has its own address (ADR 0107), the landing's under /home, read from the pages'
// routes.
builder.Services.AddSingleton(SiteAddresses.From("home", typeof(App).Assembly));
builder.Services.AddSingleton<GuideDoor>(); // "Take the tour" and any other button that asks the guide in the corner
builder.Services.AddSingleton<AgentWindows>(); // a showroom's live window shows the business the guide asks (F127.4.2)

// The showroom's demos (F127.1): the rail, the phone's chips and the start's covers are drawn from them.
builder.Services.AddSingleton(SiteDemos.All);

await builder.Build().RunAsync();
