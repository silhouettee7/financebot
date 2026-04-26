using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinBot.Bll.Interfaces;

public interface IUnitOfWork<TContext> : IDisposable, IAsyncDisposable where TContext : DbContext
{
    IGenericRepository<User, Guid, TContext> Users { get; }
    IGenericRepository<Group, Guid, TContext> Groups { get; }
    IGenericRepository<Saving, Guid, TContext> Savings { get; }
    IGenericRepository<Account, int, TContext> Accounts { get; }
    IGenericRepository<Expense, int, TContext> Expenses { get; }

    IDbContextTransaction? CurrentTransaction { get; }

    Task<Result> SaveChangesAsync();

    Task<IDbContextTransaction> BeginDbTransactionAsync();
}