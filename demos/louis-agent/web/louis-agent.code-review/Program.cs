using LouisAgent.Packs.Files.Web;
using LouisAgent.Packs.Git.Web;
using LouisAgent.Packs.Git.Web.Branches;
using LouisAgent.Packs.Git.Web.Changes;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LouisAgent.CodeReview;
using LouisAgent.CodeReview.Layout;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Evals;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Frames;
using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// louis-agent.api serves this app, so the API is on the same origin.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ChatSettings>();
builder.Services.AddSingleton<ChatStore>();
builder.Services.AddScoped<AgentApi>();
builder.Services.AddScoped<GitState>();
builder.Services.AddScoped<GitHistory>();
builder.Services.AddScoped<CurrencyState>();
builder.Services.AddSingleton(new ChatText()); // the shared components' words, in English
builder.Services.AddLouisAgentAccounts(); // sign in, register and the profile, when the API has ACCOUNTS_SQL_URL
builder.Services.AddSingleton(new SiteFrames(Admin: typeof(AdminLayout), Account: typeof(AccountLayout)));
builder.Services.AddLouisAgentEvals();

// Which component draws each screen: the plain table, then the packs' own, then ours if we ever write one.
var views = new PackViews();
GitWebViews.AddTo(views);
FilesWebViews.AddTo(views);
builder.Services.AddSingleton(views);

builder.Services.AddSingleton(new ChatHandoff("chat")); // a screen's Review button opens the chat here, with a draft

// Every place on the site's pages has its own address (ADR 0107), the landing's under /home, read from the pages' routes.
builder.Services.AddSingleton(SiteAddresses.From("home", typeof(App).Assembly));
builder.Services.AddSingleton<GuideDoor>(); // "Take the tour" and any other button that asks the guide in the corner

await builder.Build().RunAsync();
