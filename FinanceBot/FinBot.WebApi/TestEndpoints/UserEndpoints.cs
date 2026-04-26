using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.WebApi.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.WebApi.TestEndpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Users")
            .WithTags("Users")
            .WithOpenApi();

        group.MapGet("/{id:long}", GetUserTg)
            .WithName("GetUserTg")
            .WithDescription("Получить пользователя по Telegram ID")
            .Produces<User>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}", GetUserGuid)
            .WithName("GetUserGuid")
            .WithDescription("Получить пользователя по GUID")
            .Produces<User>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithDescription("Создать нового пользователя")
            .Produces<User>()
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/ensure", GetOrCreateUser)
            .WithName("GetOrCreateUser")
            .WithDescription("Получить существующего пользователя или создать нового")
            .Produces<User>()
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/{userId:Guid}/expenses", AddExpense)
            .WithName("AddUserExpense")
            .WithDescription("Добавить расход пользователя и вернуть новый баланс счёта")
            .Produces<NewBalanceResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);
    }


    private static async Task<IResult> GetUserTg(
        long id,
        IUserService userService)
    {
        var result = await userService.GetUserByTgIdAsync(id);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> GetUserGuid(
        Guid id,
        IUserService userService)
    {
        var result = await userService.GetUserByGuidIdAsync(id);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> CreateUser(
        [FromBody] CreateUserRequest request,
        IUserService userService)
    {
        var result = await userService.CreateUserAsync(request.TgId, request.DisplayName);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> GetOrCreateUser(
        [FromBody] CreateUserRequest request,
        IUserService userService)
    {
        var result = await userService.GetOrCreateUserAsync(request.TgId, request.DisplayName);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> AddExpense(
        Guid userId,
        [FromBody] AddExpenseRequest request,
        IUserService userService)
    {
        var result = await userService.AddExpenseAsync(
            userId,
            request.GroupId,
            request.Amount,
            request.Category
        );

        return result.IsSuccess
            ? Results.Ok(new NewBalanceResponse(result.Data))
            : result.ToErrorHttpResult();
    }
}

public record CreateUserRequest(long TgId, string DisplayName);

public record AddExpenseRequest(Guid GroupId, decimal Amount, ExpenseCategory Category);

public record NewBalanceResponse(decimal NewBalance);