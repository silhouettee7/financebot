using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FinBot.ExcelService.Repositories;

public class ExpenseRepository(PDbContext dbContext) : IExpenseRepository
{
    public Task<List<Expense>> GetForUserInGroupAsync(Guid userId, Guid groupId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default) =>
        dbContext.Expenses.AsNoTracking()
            .Where(e => e.UserId == userId && e.GroupId == groupId && e.Date >= from && e.Date < to)
            .ToListAsync(cancellationToken);

    public Task<List<Expense>> GetForGroupAsync(Guid groupId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default) =>
        dbContext.Expenses.AsNoTracking()
            .Where(e => e.GroupId == groupId && e.Date >= from && e.Date < to)
            .ToListAsync(cancellationToken);
}
