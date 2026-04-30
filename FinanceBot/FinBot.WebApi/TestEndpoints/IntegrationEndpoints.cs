using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Domain.Events;
using FinBot.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.WebApi.TestEndpoints;

public static class IntegrationEndpoints
{
    public static void MapIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var mapGroup = app.MapGroup("/Integrations")
            .WithTags("Integrations")
            .WithOpenApi();

        mapGroup.MapPost("/Table", GetTable)
            .WithName("GetTable")
            .WithDescription("Получить Excel-таблицу расходов для группы или конкретного пользователя")
            .Produces(StatusCodes.Status200OK,
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        mapGroup.MapPost("/Table/Generate", GenerateTable)
            .WithName("GenerateTable")
            .WithDescription("Поставить в очередь генерацию Excel-таблицы расходов")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        mapGroup.MapPost("/Diagram", GetDiagram)
            .WithName("GetDiagram")
            .WithDescription("Получить диаграмму расходов по категориям для группы или пользователя")
            .Produces(StatusCodes.Status200OK, contentType: "image/png")
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        mapGroup.MapPost("/Diagram/Generate", GenerateDiagram)
            .WithName("GenerateDiagram")
            .WithDescription("Поставить в очередь генерацию диаграммы расходов по категориям")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        mapGroup.MapPost("/LineChart", GetLineChart)
            .WithName("GetLineChart")
            .WithDescription("Получить линейный график динамики расходов для группы или пользователя")
            .Produces(StatusCodes.Status200OK, contentType: "image/png")
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        mapGroup.MapPost("/LineChart/Generate", GenerateLineChart)
            .WithName("GenerateLineChart")
            .WithDescription("Поставить в очередь генерацию линейного графика расходов")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetTable(IIntegrationsService integrationsService, [FromQuery] Guid groupId,
        [FromQuery] Guid? userId)
    {
        var result = userId is null
            ? await integrationsService.GetExcelTableForGroup(groupId)
            : await integrationsService.GetExcelTableForUserInGroup(userId.Value, groupId);


        return result.IsSuccess
            ? Results.File(
                fileContents: result.Data,
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: $"expenses_{DateTime.Now:yyyyMMdd}.xlsx"
            )
            : Results.Problem(result.ErrorMessage);
    }

    private static async Task<IResult> GenerateTable(
        IReportProducer producer,
        [FromQuery] Guid groupId, [FromQuery] Guid? userId)
    {
        var evt = new ReportGenerationEvent
        {
            GroupId = groupId,
            UserId = userId,
            Type = ReportType.ExcelTable
        };

        var result = await producer.QueueReportGenerationAsync(evt);

        return result.IsSuccess
            ? Results.Accepted(value: "Request queued")
            : Results.Problem(result.ErrorMessage);
    }

    private static async Task<IResult> GetDiagram(IIntegrationsService integrationsService, [FromQuery] Guid groupId,
        [FromQuery] Guid? userId)
    {
        var result = userId is null
            ? await integrationsService.GetDiagramForGroup(groupId)
            : await integrationsService.GetDiagramForUserInGroup(userId.Value, groupId);


        return result.IsSuccess
            ? Results.File(
                fileContents: result.Data,
                contentType: "image/png",
                fileDownloadName: $"diagram_{DateTime.Now:yyyyMMdd}.xlsx"
            )
            : Results.Problem(result.ErrorMessage);
    }

    private static async Task<IResult> GenerateDiagram(
        IReportProducer producer,
        [FromQuery] Guid groupId, [FromQuery] Guid? userId)
    {
        var evt = new ReportGenerationEvent
        {
            GroupId = groupId,
            UserId = userId,
            Type = ReportType.CategoryChart
        };

        var result = await producer.QueueReportGenerationAsync(evt);

        return result.IsSuccess
            ? Results.Accepted(value: "Request queued")
            : Results.Problem(result.ErrorMessage);
    }

    private static async Task<IResult> GetLineChart(IIntegrationsService integrationsService, [FromQuery] Guid groupId,
        [FromQuery] Guid? userId)
    {
        var result = userId is null
            ? await integrationsService.GetLineChartForGroup(groupId)
            : await integrationsService.GetLineChartForUserInGroup(userId.Value, groupId);


        return result.IsSuccess
            ? Results.File(
                fileContents: result.Data,
                contentType: "image/png",
                fileDownloadName: $"lineChart_{DateTime.Now:yyyyMMdd}.xlsx"
            )
            : Results.Problem(result.ErrorMessage);
    }

    private static async Task<IResult> GenerateLineChart(
        IReportProducer producer,
        [FromQuery] Guid groupId, [FromQuery] Guid? userId)
    {
        var evt = new ReportGenerationEvent
        {
            GroupId = groupId,
            UserId = userId,
            Type = ReportType.LineChart
        };

        var result = await producer.QueueReportGenerationAsync(evt);

        return result.IsSuccess
            ? Results.Accepted(value: "Request queued")
            : Results.Problem(result.ErrorMessage);
    }
}