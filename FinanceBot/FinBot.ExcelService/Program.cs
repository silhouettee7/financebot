using FinBot.Dal;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using FinBot.ExcelService;
using FinBot.ExcelService.Reports;
using FinBot.ExcelService.Services;
using FinBot.MinIOS3;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services
    .AddMinioS3(configuration)
    .AddPostgresDb(configuration)
    .AddExcelReportPipeline(configuration);

var app = builder.Build();

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