using BankMore.Transferencias.Domain.Entities;

namespace BankMore.Transferencias.Domain.Repositories;

public interface ITransferRepository
{
    Task<Transfer?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken = default);
    Task<Transfer> CreateAsync(Transfer transfer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transfer transfer, CancellationToken cancellationToken = default);
    Task<bool> ExistsByRequestIdAsync(string requestId, CancellationToken cancellationToken = default);
}

