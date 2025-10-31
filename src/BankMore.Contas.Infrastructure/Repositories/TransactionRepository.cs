using BankMore.Contas.Domain.Entities;
using BankMore.Contas.Domain.Enums;
using Dapper;
using System.Data;

namespace BankMore.Contas.Infrastructure.Repositories;

public class TransactionRepository : Domain.Repositories.ITransactionRepository
{
    private readonly IDbConnection _connection;

    public TransactionRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Transaction?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, AccountId, RequestId, Amount, Type, CreatedAt
            FROM Transactions
            WHERE RequestId = @RequestId";

        var result = await _connection.QueryFirstOrDefaultAsync<TransactionDto>(
            new CommandDefinition(sql, new { RequestId = requestId }, cancellationToken: cancellationToken));

        return result?.ToDomain();
    }

    public async Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Transactions (AccountId, RequestId, Amount, Type, CreatedAt)
            VALUES (@AccountId, @RequestId, @Amount, @Type, @CreatedAt);
            SELECT last_insert_rowid();";

        var transactionDto = TransactionDto.FromDomain(transaction);
        var id = await _connection.QuerySingleAsync<int>(
            new CommandDefinition(sql, transactionDto, cancellationToken: cancellationToken));

        transaction.Id = id;
        return transaction;
    }

    public async Task<bool> ExistsByRequestIdAsync(string requestId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Transactions
            WHERE RequestId = @RequestId";

        var count = await _connection.QuerySingleAsync<int>(
            new CommandDefinition(sql, new { RequestId = requestId }, cancellationToken: cancellationToken));

        return count > 0;
    }
}

internal class TransactionDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public char Type { get; set; }
    public DateTime CreatedAt { get; set; }

    public Transaction ToDomain()
    {
        var transaction = Transaction.Create(
            AccountId,
            RequestId,
            Amount,
            (TransactionType)Type);

        transaction.Id = Id;
        transaction.CreatedAt = CreatedAt;

        return transaction;
    }

    public static TransactionDto FromDomain(Transaction transaction)
    {
        return new TransactionDto
        {
            AccountId = transaction.AccountId,
            RequestId = transaction.RequestId,
            Amount = transaction.Amount,
            Type = (char)transaction.Type,
            CreatedAt = transaction.CreatedAt
        };
    }
}

