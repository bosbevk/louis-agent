using LouisAgent.Starter;
using LouisAgent.Starter.Layout;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Admin;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Frames;
using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// louis-agent.api serves this app, so the API is on the same origin.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ChatSettings>();
builder.Services.AddSingleton<ChatStore>();
builder.Services.AddScoped<AgentApi>();
builder.Services.AddScoped<CurrencyState>();
builder.Services.AddSingleton(new ChatText()); // the frame's words, in English; a site can fill in its own
builder.Services.AddLouisAgentAccounts(); // sign in, register and the profile, when the API has ACCOUNTS_SQL_URL
builder.Services.AddSingleton(new SiteFrames(Admin: typeof(AdminLayout), Account: typeof(AccountLayout)));
builder.Services.AddSingleton(new ChatHandoff("chat"));
EvalPages.AddTo(builder.Services);

// Which component draws each screen: the plain table, then a pack's own, then yours. A pack's view is added here.
builder.Services.AddSingleton(new PackViews());

await builder.Build().RunAsync();
