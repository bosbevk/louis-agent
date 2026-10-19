using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Usage;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Text;
using LouisAgent.Web.Git;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LouisAgent.Web;

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
builder.Services.AddSingleton(new LouisAgent.Web.Shared.Text.ChatText());   // the shared components' words, in English

await builder.Build().RunAsync();
