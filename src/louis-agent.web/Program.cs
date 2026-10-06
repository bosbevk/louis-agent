using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using louis_agent.web;
using louis_agent.web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// louis-agent.api serves this app, so the API is on the same origin.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<Settings>();
builder.Services.AddSingleton<ChatStore>();
builder.Services.AddScoped<AgentApi>();
builder.Services.AddScoped<GitState>();
builder.Services.AddScoped<GitHistory>();

await builder.Build().RunAsync();
