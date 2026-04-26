using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FinBot.Bll.Implementation.Services;

public class UserService(
    PDbContext dbContext,
    ILogger<UserService> logger) : IUserService
{
    public async Task<Result<User>> GetUserByTgIdAsync(long userId)
    {
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == userId);
            if (user is null)
            {
                logger.LogError("User with tgId {userId} does not exist", userId);
                return Result<User>.Failure("User not exist", ErrorType.NotFound);
            }

            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during get user: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<User>.Failure(ex.Message);
        }
    }

    public async Task<Result<User>> GetUserByGuidIdAsync(Guid userId)
    {
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                logger.LogError("User {userId} does not exist", userId);
                return Result<User>.Failure("User not exist", ErrorType.NotFound);
            }

            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during get user: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<User>.Failure(ex.Message);
        }
    }

    public async Task<Result<User>> CreateUserAsync(long tgId, string displayName)
    {
        try
        {
            var isExist = await dbContext.Users.AnyAsync(u => u.TelegramId == tgId);
            if (isExist)
            {
                return Result<User>.Failure("User with such id already exists", ErrorType.Conflict);
            }

            var newUser = new User
            {
                TelegramId = tgId,
                DisplayName = displayName
            };

            await dbContext.Users.AddAsync(newUser);
            await dbContext.SaveChangesAsync();

            return Result<User>.Success(newUser);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during create user: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<User>.Failure(ex.Message);
        }
    }

    public async Task<Result<User>> GetOrCreateUserAsync(long tgId, string displayName)
    {
        try
        {
            var existedUser = await dbContext.Users.FirstOrDefaultAsync(u => tgId == u.TelegramId);
            if (existedUser != null)
            {
                return Result<User>.Success(existedUser);
            }

            var newUser = new User
            {
                TelegramId = tgId,
                DisplayName = displayName
            };

            await dbContext.Users.AddAsync(newUser);
            await dbContext.SaveChangesAsync();

            return Result<User>.Success(newUser);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            var existedUser = await dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == tgId);
            return Result<User>.Success(existedUser!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during get or create user: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<User>.Failure(ex.Message);
        }
    }

    public async Task<Result<decimal>> AddExpenseAsync(
        Guid userId,
        Guid groupId,
        decimal amount,
        ExpenseCategory category)
    {
        try
        {
            var user = await dbContext.Users
                .Include(u => u.Accounts)
                .ThenInclude(a => a.Expenses)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                logger.LogError("User {userId} does not exist", userId);
                return Result<decimal>.Failure("User not exist", ErrorType.NotFound);
            }

            var groupExists  = await dbContext.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
            {
                logger.LogError("Group {groupId} does not exist", groupId);
                return Result<decimal>.Failure("Group does not exist", ErrorType.NotFound);
            }

            var account = user.Accounts.FirstOrDefault(a => a.GroupId == groupId);
            if (account is null)
            {
                logger.LogError("User {userId} doesn't has an Account in group {groupId}", userId, groupId);
                return Result<decimal>.Failure("User not exist", ErrorType.NotFound);
            }

            var newExpense = new Expense
            {
                Category = category,
                Amount = amount,
                Date = DateTime.UtcNow
            };

            account.Balance -= amount;
            account.Expenses.Add(newExpense);

            await dbContext.SaveChangesAsync();

            return Result<decimal>.Success(account.Balance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during add expense: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<decimal>.Failure(ex.Message);
        }
    }
}