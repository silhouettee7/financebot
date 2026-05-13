using System.Text.Json.Serialization;
using FinBot.App;
using FinBot.App.Endpoints;
using FinBot.App.Extensions;
using FinBot.App.GroupJob;
using FinBot.Dal;
using FinBot.Dal.DbContexts;
using FinBot.Integrations;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Filter.ByExcluding(logEvent => 
            logEvent.Properties.ContainsKey("RequestPath") && 
            logEvent.Properties["RequestPath"].ToString().Contains("/hf"))
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341");
});

// игнорировать циклы при возврате json
services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

services
    .AddPostgresDb(configuration)
    .AddHangfire(configuration)
    .AddOpenApi()
    .AddRedisCacheIntegration(configuration);

var app = builder.Build();

app.UseHangfireDashboard();

if (app.Environment.IsDevelopment())
{
    // Scalar
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar");

    // Hangfire
    app.MapHangfireDashboard("/hf", new DashboardOptions
    {
        Authorization = [new HangfireAllowAllAuthFilter()]
    });
    
    app.MapUserEndpoints();
    app.MapGroupEndpoints();
    app.MapBackgroundEndpoints();
}

app.MapGet("/", () => "Hello World!");

AddDailyJob(app);

MigrateDatabase(app);

app.Run();
return;

static void MigrateDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var db = scope.ServiceProvider.GetRequiredService<PDbContext>();

    if (db.Database.GetPendingMigrations().Any())
    {
        db.Database.Migrate();
    }
}

static void AddDailyJob(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        var mskTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

        recurringJobManager.AddOrUpdate<GroupJobDispatcher>(
            "main-group-dispatch-job",
            dispatcher => dispatcher.DispatchTasksAsync(),
            Cron.Daily(0, 0),
            new RecurringJobOptions
            {
                TimeZone = mskTimeZone
            }
        );
    }
}