using FinBot.Dal;
using FinBot.Integrations;
using FinBot.WebApi.Extensions;
using Hangfire;
using Serilog;
using Scalar.AspNetCore;

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

services.AddPostgresDb(configuration);
services.AddTelegram(configuration);
services.AddOpenApi();
services.AddKafkaIntegration();
services.AddRedisCacheIntegration(configuration);

var app = builder.Build();

app.UseHangfireDashboard();

if (app.Environment.IsDevelopment())
{
    // Scalar
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar");
}

app.Run();