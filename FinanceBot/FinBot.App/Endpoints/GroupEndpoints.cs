using FinBot.App.Extensions;
using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.App.Endpoints;

public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var mapGroup = app.MapGroup("/Groups")
            .WithTags("Group")
            .WithOpenApi();

        mapGroup.MapGet("/", GetAllGroupsAsync)
            .WithName("GetAllGroups")
            .WithDescription("Получить список всех групп")
            .Produces<List<Group>>();

        mapGroup.MapGet("/{groupId:Guid}", GetGroupByIdAsync)
            .WithName("GetGroupById")
            .WithDescription("Получить группу по ID")
            .Produces<Group>()
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPost("/New/Guid", NewGroupWithGuidUser)
            .WithName("CreateGroup")
            .WithDescription("Создать новую группу с указанием создателя по GUID")
            .Produces<Group>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPost("/RecalculateAllocations", RecalculateAllocations)
            .WithName("RecalculateAllocations")
            .WithDescription("Пересчитать распределение бюджета между участниками группы")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPost("/AddUser", AddUser)
            .WithName("AddUserToGroup")
            .WithDescription("Добавить пользователя в группу и вернуть его счёт")
            .Produces<Account>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPost("/RemoveUser", RemoveUser)
            .WithName("RemoveUserFromGroup")
            .WithDescription("Удалить пользователя из группы")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPatch("/ChangeGoal", ChangeGoal)
            .WithName("ChangeGroupGoal")
            .WithDescription("Изменить цель накоплений группы")
            .Produces<Saving>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        mapGroup.MapPatch("/", UpdateGroup)
            .WithName("UpdateGroup")
            .WithDescription("Обновить параметры группы (название, пополнение, стратегии)")
            .Produces<Group>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllGroupsAsync(IGroupService groupService)
    {
        var result = await groupService.GetGroupsAsync();

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> GetGroupByIdAsync([FromQuery] Guid groupId, IGroupService groupService)
    {
        var result = await groupService.GetGroupByIdAsync(groupId);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> NewGroupWithGuidUser(
        [FromQuery] Guid userId,
        [FromBody] CreateGroupDto dto,
        IGroupService groupService)
    {
        var result = await groupService.CreateGroupAsync(dto.GroupName,
            userId,
            dto.Replenishment,
            dto.GroupSavingStrategy,
            dto.AccountSavingStrategy,
            dto.DebtStrategy,
            dto.SavingTargetName,
            dto.SavingTargetAmount);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> RecalculateAllocations(
        [FromQuery] Guid groupId,
        [FromBody] RecalculateAllocationsDto dto,
        IGroupService groupService)
    {
        var result = await groupService.RecalculateMonthlyAllocationsAsync(groupId, dto.Allocations);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> AddUser(
        [FromQuery] Guid groupId,
        [FromBody] AddUserToGroupDto dto,
        IGroupService groupService)
    {
        var result = await groupService.AddUserToGroupAsync(
            groupId,
            dto.UserId,
            dto.UserRole,
            dto.OldUsersAllocations,
            dto.NewUserAllocation,
            dto.UserSavingStrategy);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> RemoveUser(
        [FromQuery] Guid groupId,
        [FromBody] RemoveUserDto dto,
        IGroupService groupService)
    {
        var result = await groupService.RemoveUserFromGroupAsync(groupId, dto.UserTgId, dto.OldUsersAllocations);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> ChangeGoal(
        [FromQuery] Guid groupId,
        [FromQuery] string targetName,
        [FromQuery] decimal targetCost,
        IGroupService groupService)
    {
        var result = await groupService.ChangeGoalAsync(groupId, targetName, targetCost);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToErrorHttpResult();
    }

    private static async Task<IResult> UpdateGroup(
        Guid groupId,
        [FromBody] UpdateGroupDto dto,
        IGroupService groupService)
    {
        var result = await groupService.UpdateGroupAsync(
            groupId,
            dto.Name,
            dto.MonthlyReplenishment,
            dto.SavingStrategy,
            dto.DebtStrategy);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToErrorHttpResult();
    }
}

public record CreateGroupDto(
    string GroupName,
    decimal Replenishment,
    SavingStrategy GroupSavingStrategy,
    SavingStrategy AccountSavingStrategy,
    DebtStrategy DebtStrategy,
    string? SavingTargetName,
    decimal? SavingTargetAmount);

public record RecalculateAllocationsDto(decimal[] Allocations);

public record AddUserToGroupDto(
    Guid UserId,
    Role UserRole,
    decimal[] OldUsersAllocations,
    decimal NewUserAllocation,
    SavingStrategy UserSavingStrategy);

public record UpdateGroupDto(
    string? Name,
    decimal? MonthlyReplenishment,
    SavingStrategy? SavingStrategy,
    DebtStrategy? DebtStrategy);

public record RemoveUserDto(
    long UserTgId,
    decimal[] OldUsersAllocations);