using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IUserService
{
    Task<Result<User>> CreateUserAsync(long tgId, string displayName);
    Task<Result<User>> GetOrCreateUserAsync(long tgId, string displayName);
    Task<Result<decimal>> AddExpenseAsync(
        Guid userId,
        Guid groupId,
        decimal amount,
        ExpenseCategory category);

    Task<Result<User>> GetUserByGuidIdAsync(Guid userId);
    Task<Result<User>> GetUserByTgIdAsync(long userId);
}