using MudBlazor.Services;
using Redis.OM;
using RedisStatusPage.Core;
using RedisStatusPage.Core.Contracts;
using RedisStatusPage.Core.Services;
using RedisStatusPage.Data;
using RedisStatusPage.Services;
using StackExchange.Redis;

// Create app builder
var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Services.Configure<MonitorOptions>(builder.Configuration);

// Add Blazor Server services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddHttpClient();

// Add services
builder.Services.AddScoped<GlobalAppState>();

builder.Services.AddSingleton<INetProbe, NetProbe>();
builder.Services.AddSingleton<IIncidentsService, IncidentsService>();
builder.Services.AddSingleton<IStatisticsService, StatisticsService>((IServiceProvider provider) =>
{
    var options = builder.Configuration.Get<MonitorOptions>();
    var cnMultiplexer = provider.GetRequiredService<ConnectionMultiplexer>();
    var cnProvider = provider.GetRequiredService<RedisConnectionProvider>();

    return new StatisticsService(cnMultiplexer, cnProvider)
    {
        GraphLastSecond = options.GraphLastSeconds
    };
});

// add hosted service to check for service health periodically
builder.Services.AddHostedService<HealthCheckProbe>();

// add Redis services
builder.Services.AddSingleton((IServiceProvider provider) =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var options = config.Get<MonitorOptions>();
    
    return ConnectionMultiplexer.Connect(options.RedisUri, new StreamWriter(Console.OpenStandardOutput()));
});
builder.Services.AddSingleton((IServiceProvider provider) =>
{
    return new RedisConnectionProvider(provider.GetRequiredService<ConnectionMultiplexer>());
});

// Build server
var app = builder.Build();

// initialize index
await app.Services
    .GetRequiredService<IStatisticsService>()
    .CreateIndexIfNotExists();
await app.Services
    .GetRequiredService<IIncidentsService>()
    .CreateIndexIfNotExists();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();
