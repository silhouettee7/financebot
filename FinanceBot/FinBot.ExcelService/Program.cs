using FinBot.Dal;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using FinBot.ExcelService;
using FinBot.ExcelService.Reports;
using FinBot.ExcelService.Services;
using FinBot.MinIOS3;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341");
});

builder.Services
    .AddMinioS3(configuration)
    .AddPostgresDb(configuration)
    .AddExcelReportPipeline(configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar");
}

app.MapPost("/reports", async (
    Guid userId, Guid groupId, ReportType reportType, ExcelType excelType, TimeInterval timeInterval,
    IReportService reports, CancellationToken ct) =>
{
    var result = await reports.GenerateAndStoreAsync(
        new ReportRequest(userId, groupId, reportType, excelType, timeInterval), ct);

    return result.IsSuccess
        ? Results.Ok(new { fileKey = result.Data })
        : result.ErrorType == ErrorType.NotFound
            ? Results.NotFound(result.ErrorMessage)
            : Results.Problem(result.ErrorMessage);
});

app.Run();
