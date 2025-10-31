using BankMore.Contas.Domain.Entities;
using BankMore.Contas.Domain.Enums;
using BankMore.Contas.Domain.Repositories;
using BankMore.Contas.Domain.ValueObjects;
using Dapper;
using System.Data;

namespace BankMore.Contas.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly IDbConnection _connection;

    public AccountRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Cpf, AccountNumber, Name, PasswordHash, Status, CreatedAt, UpdatedAt
            FROM Accounts
            WHERE Id = @Id";

        var result = await _connection.QueryFirstOrDefaultAsync<AccountDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return result?.ToDomain();
    }

    public async Task<Account?> GetByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Cpf, AccountNumber, Name, PasswordHash, Status, CreatedAt, UpdatedAt
            FROM Accounts
            WHERE AccountNumber = @AccountNumber";

        var result = await _connection.QueryFirstOrDefaultAsync<AccountDto>(
            new CommandDefinition(sql, new { AccountNumber = accountNumber.Value }, cancellationToken: cancellationToken));

        return result?.ToDomain();
    }

    public async Task<Account?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Cpf, AccountNumber, Name, PasswordHash, Status, CreatedAt, UpdatedAt
            FROM Accounts
            WHERE Cpf = @Cpf";

        var result = await _connection.QueryFirstOrDefaultAsync<AccountDto>(
            new CommandDefinition(sql, new { Cpf = cpf.Value }, cancellationToken: cancellationToken));

        return result?.ToDomain();
    }

    public async Task<Account?> GetByCpfOrAccountNumberAsync(string login, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Cpf, AccountNumber, Name, PasswordHash, Status, CreatedAt, UpdatedAt
            FROM Accounts
            WHERE Cpf = @Login OR AccountNumber = @Login";

        var result = await _connection.QueryFirstOrDefaultAsync<AccountDto>(
            new CommandDefinition(sql, new { Login = login }, cancellationToken: cancellationToken));

        return result?.ToDomain();
    }

    public async Task<Account> CreateAsync(Account account, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Accounts (Cpf, AccountNumber, Name, PasswordHash, Status, CreatedAt)
            VALUES (@Cpf, @AccountNumber, @Name, @PasswordHash, @Status, @CreatedAt);
            SELECT last_insert_rowid();";

        var accountDto = AccountDto.FromDomain(account);
        var id = await _connection.QuerySingleAsync<int>(
            new CommandDefinition(sql, accountDto, cancellationToken: cancellationToken));

        return account.WithId(id);
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Accounts
            SET Status = @Status, UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                Id = account.Id,
                Status = (int)account.Status,
                UpdatedAt = account.UpdatedAt
            }, cancellationToken: cancellationToken));
    }

    public async Task<decimal> GetBalanceAsync(int accountId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                COALESCE(SUM(CASE WHEN Type = 'C' THEN Amount ELSE -Amount END), 0) AS Balance
            FROM Transactions
            WHERE AccountId = @AccountId";

        var balance = await _connection.QuerySingleOrDefaultAsync<decimal?>(
            new CommandDefinition(sql, new { AccountId = accountId }, cancellationToken: cancellationToken));

        return balance ?? 0;
    }
}

internal class AccountDto
{
    public int Id { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Account ToDomain()
    {
        var cpfValueObject = BankMore.Contas.Domain.ValueObjects.Cpf.Create(this.Cpf);
        var accountNumberValueObject = BankMore.Contas.Domain.ValueObjects.AccountNumber.FromString(this.AccountNumber);
        
        var account = Account.Create(
            cpfValueObject,
            accountNumberValueObject,
            Name,
            PasswordHash);

        account = account.WithId(Id);
        account.Status = (AccountStatus)Status;
        account.CreatedAt = CreatedAt;
        account.UpdatedAt = UpdatedAt;

        return account;
    }

    public static AccountDto FromDomain(Account account)
    {
        return new AccountDto
        {
            Cpf = account.Cpf.Value,
            AccountNumber = account.AccountNumber.Value,
            Name = account.Name,
            PasswordHash = account.PasswordHash,
            Status = (int)account.Status,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}

