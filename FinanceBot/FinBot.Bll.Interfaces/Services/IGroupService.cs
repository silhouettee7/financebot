using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IGroupService
{
    Task<Result<Group>> CreateGroupAsync(
        string groupName,
        Guid creatorId,
        decimal replenishment,
        SavingStrategy groupSavingStrategy,
        SavingStrategy accountSavingStrategy,
        DebtStrategy debtStrategy,
        string? savingTargetName,
        decimal? savingTargetAmount);

    Task<Result<Group>> UpdateGroupAsync(
        Guid groupId,
        string? name,
        decimal? monthlyReplenishment,
        SavingStrategy? savingStrategy,
        DebtStrategy? debtStrategy);

    Task<Result> RecalculateMonthlyAllocationsAsync(
        Guid groupId,
        decimal[] allocations,
        bool saveChanges = true);

    Task<Result<Saving>> ChangeGoalAsync(
        Guid groupId,
        string savingTargetName,
        decimal savingTargetAmount);

    Task<Result<Account>> AddUserToGroupAsync(
        Guid groupId,
        Guid userId,
        Role newUserRole,
        decimal[] oldUsersAllocations,
        decimal newUserAllocation,
        SavingStrategy newUserSavingStrategy);

    Task<Result> RemoveUserFromGroupAsync(
        Guid groupId,
        long userTgId,
        decimal[] leftUsersAllocations);
}