using LouisAgent.Packs.Git.Web.Branches;
using LouisAgent.Packs.Git.Web.Changes;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LouisAgent.CodeReview;
using LouisAgent.Web.Components.Areas;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// louis-agent.api serves this app, so the API is on the same origin.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<GitState>();
builder.Services.AddScoped<GitHistory>();
// The chat, the admin with the packs' views, the accounts and the eval studio (F127.6).
builder.Services.AddSiteAreas(CodeReviewAreas.Name);

await builder.Build().RunAsync();
