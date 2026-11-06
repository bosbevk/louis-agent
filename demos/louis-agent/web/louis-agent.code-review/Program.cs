using LouisAgent.CodeReview.Git;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LouisAgent.CodeReview;
using LouisAgent.Web.Api;
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
builder.Services.AddSingleton(new ChatText());   // the shared components' words, in English

await builder.Build().RunAsync();
