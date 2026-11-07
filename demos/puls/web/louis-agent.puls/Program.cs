using LouisAgent.Puls.Text;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LouisAgent.Packs.Recipes.Web;
using LouisAgent.Puls;
using LouisAgent.Puls.Admin;
using LouisAgent.Puls.Content;
using LouisAgent.Puls.Layout;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// louis-agent.api serves this app, so the API is on the same origin.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ChatSettings>();
builder.Services.AddSingleton<ChatStore>();
builder.Services.AddScoped<AgentApi>();
builder.Services.AddScoped<CurrencyState>();
EvalPages.AddTo(builder.Services, PulsSites.Kinds);
builder.Services.AddSingleton(PulsText.Afrikaans); // Puls speaks Afrikaans
builder.Services.AddSingleton(new SiteFrames(Admin: typeof(AdminLayout)));

// Which component draws each screen: the plain table, then the packs' own, then ours if we ever write one.
var views = new PackViews();
RecipeWebViews.AddTo(views);
builder.Services.AddSingleton(views);

await builder.Build().RunAsync();
