using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.WebApi.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.WebApi.TestEndpoints;

public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var mapGroup = app.MapGroup("/Groups")
            .WithTags("Group")
            .WithOpenApi();

        mapGroup.MapGet("/", GetAllGroupsAsync)
            .Produces<List<Group>>();

        mapGroup.MapGet("/{groupId:Guid}", GetGroupByIdAsync)
            .Produces<Group>();

        mapGroup.MapPost("/New/Guid", NewGroupWithGuidUser)
            .Produces<Group>();

        mapGroup.MapPost("/RecalculateAllocations", RecalculateAllocations);

        mapGroup.MapPost("/AddUser", AddUser)
            .Produces<Account>();

        mapGroup.MapPost("/RemoveUser", RemoveUser)
            .Produces<Account>();

        mapGroup.MapPatch("/ChangeGoal", ChangeGoal)
            .Produces<Saving>();

        mapGroup.MapPatch("/", UpdateGroup)
            .Produces<Group>();
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