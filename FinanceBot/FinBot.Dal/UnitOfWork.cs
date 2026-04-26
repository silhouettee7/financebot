using FinBot.Bll.Interfaces;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace FinBot.Dal;

public class UnitOfWork<TContext>(TContext context, ILogger<UnitOfWork<TContext>> logger)
    : IUnitOfWork<TContext>, IDisposable, IAsyncDisposable where TContext : DbContext
{
    public IGenericRepository<User, Guid, TContext> Users { get; } =
        new GenericRepository<User, Guid, TContext>(context);

    public IGenericRepository<Group, Guid, TContext> Groups { get; } =
        new GenericRepository<Group, Guid, TContext>(context);

    public IGenericRepository<Saving, Guid, TContext> Savings { get; } =
        new GenericRepository<Saving, Guid, TContext>(context);

    public IGenericRepository<Account, int, TContext> Accounts { get; } =
        new GenericRepository<Account, int, TContext>(context);

    public IDbContextTransaction? CurrentTransaction => context.Database.CurrentTransaction;

    public async Task<Result> SaveChangesAsync()
    {
        try
        {
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError("Something went wrong during save changes: {errorMessage}", ex.Message);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<IDbContextTransaction> BeginDbTransactionAsync()
    {
        return await context.Database.BeginTransactionAsync();
    }

    public void Dispose()
    {
        CastAndDispose(context);

        return;

        static void CastAndDispose(IAsyncDisposable resource)
        {
            if (resource is IDisposable resourceDisposable)
                resourceDisposable.Dispose();
            else
                _ = resource.DisposeAsync().AsTask();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
    }
}