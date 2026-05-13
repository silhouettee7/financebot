using FinBot.Bll.Interfaces.Services;

namespace FinBot.App.Endpoints;

public static class BackgroundEndpoints
{
    public static void MapBackgroundEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Background")
            .WithTags("Background Jobs")
            .WithOpenApi();

        group.MapPost("/Groups/{groupId:guid}/monthly-refresh", TriggerMonthlyRefresh)
            .WithName("TriggerMonthlyGroupRefresh")
            .WithDescription("Вручную запустить ежемесячное обновление данных группы")
            .Produces<string>()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/Groups/{groupId:guid}/daily-recalculate", TriggerDailyRecalculate)
            .WithName("TriggerDailyAccountsRecalculate")
            .WithDescription("Вручную запустить ежедневный пересчёт балансов счетов группы")
            .Produces<string>()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> TriggerMonthlyRefresh(
        Guid groupId,
        IGroupBackgroundService backgroundService)
    {
        var result = await backgroundService.MonthlyGroupRefreshAsync(groupId);

        return result.IsSuccess
            ? Results.Ok($"Monthly refresh for group {groupId} triggered successfully.")
            : Results.Problem($"Monthly refresh for group {groupId} triggered failed.");
    }

    private static async Task<IResult> TriggerDailyRecalculate(
        Guid groupId,
        IGroupBackgroundService backgroundService)
    {
        var result = await backgroundService.DailyAccountsRecalculateAsync(groupId);

        return result.IsSuccess
            ? Results.Ok($"Daily recalculation for group {groupId} triggered successfully.")
            : Results.Problem($"Daily recalculation for group {groupId} triggered failed.");
    }
}
