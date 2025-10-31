using BankMore.Contas.Domain.Entities;
using BankMore.Contas.Domain.ValueObjects;

namespace BankMore.Contas.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Account?> GetByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken = default);
    Task<Account?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default);
    Task<Account?> GetByCpfOrAccountNumberAsync(string login, CancellationToken cancellationToken = default);
    Task<Account> CreateAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task<decimal> GetBalanceAsync(int accountId, CancellationToken cancellationToken = default);
}

