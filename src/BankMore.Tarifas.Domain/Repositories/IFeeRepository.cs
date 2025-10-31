using BankMore.Tarifas.Domain.Entities;

namespace BankMore.Tarifas.Domain.Repositories;

public interface IFeeRepository
{
    Task<Fee?> GetByTransferIdAsync(string transferId, CancellationToken cancellationToken = default);
    Task<Fee> CreateAsync(Fee fee, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTransferIdAsync(string transferId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Fee>> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Fee>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
}

