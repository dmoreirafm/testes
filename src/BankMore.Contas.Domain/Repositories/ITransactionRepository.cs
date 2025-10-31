using BankMore.Contas.Domain.Entities;

namespace BankMore.Contas.Domain.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken = default);
    Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<bool> ExistsByRequestIdAsync(string requestId, CancellationToken cancellationToken = default);
}

