using MudBlazor.Services;
using Plotly.Blazor;
using Redis.OM;
using RedisStatusPage.Core;
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
    var config = provider.GetRequiredService<IConfiguration>();
    var options = config.Get<MonitorOptions>();
    
    var instance = new StatisticsService(provider.GetRequiredService<ConnectionMultiplexer>(), provider.GetRequiredService<RedisConnectionProvider>());
    instance.GraphLastSecond = options.GraphLastSeconds;

    return instance;
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
var statsService = app.Services.GetRequiredService<IStatisticsService>();
await statsService.CreateIndexIfNotExists();
var incidentService = app.Services.GetRequiredService<IIncidentsService>();
await incidentService.CreateIndexIfNotExists();

// add dummy data
//var now = DateTime.Now;
//await statsService.Snapshot(now, "Prometheus", true, Random.Shared.Next(200));
//await statsService.Snapshot(now, "Grafana", true, Random.Shared.Next(200));
//await incidentService.Add(new Incident
//{
//    UnixTimestamp = DateTimeHelpers.ToUnixSeconds(now),
//    History = new()
//    {
//        new()
//        {
//            UnixTimestamp = DateTimeHelpers.ToUnixSeconds(now),
//            Status = "REPORTED",
//            Message = "Prometheus is down!"
//        }
//    }
//});

//var incident = await incidentService.Get("01GB1FQGBTHA9E2V5HZH16RNHP");
//if (incident != null)
//{
//    incident.History.Add(new IncidentResponse
//    {
//        UnixTimestamp = DateTimeHelpers.ToUnixSeconds(now),
//        Status = "RESOLVED",
//        Message = "All OK!"
//    });
//    await incidentService.Update(incident);
//}

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
