using FinBot.Domain.Models;

namespace FinBot.ExcelService.Repositories;

public interface IExpenseRepository
{
    Task<List<Expense>> GetForUserInGroupAsync(Guid userId, Guid groupId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default);

    Task<List<Expense>> GetForGroupAsync(Guid groupId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default);
}
