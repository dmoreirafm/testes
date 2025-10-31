using BankMore.Tarifas.Domain.Entities;
using BankMore.Tarifas.Domain.Repositories;
using Dapper;
using System.Data;
using System.Linq;

namespace BankMore.Tarifas.Infrastructure.Repositories;

public class FeeRepository : IFeeRepository
{
    private readonly IDbConnection _connection;

    public FeeRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Fee?> GetByTransferIdAsync(string transferId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, TransferId, AccountNumber, TransferAmount, FeeAmount, AppliedAt
            FROM Fees
            WHERE TransferId = @TransferId";

        var result = await _connection.QueryFirstOrDefaultAsync<FeeDto>(
            new CommandDefinition(sql, new { TransferId = transferId }, cancellationToken: cancellationToken));

        return result?.ToDomain();
    }

    public async Task<Fee> CreateAsync(Fee fee, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Fees (TransferId, AccountNumber, TransferAmount, FeeAmount, AppliedAt)
            VALUES (@TransferId, @AccountNumber, @TransferAmount, @FeeAmount, @AppliedAt);
            SELECT last_insert_rowid();";

        var feeDto = FeeDto.FromDomain(fee);
        var id = await _connection.QuerySingleAsync<int>(
            new CommandDefinition(sql, feeDto, cancellationToken: cancellationToken));

        fee.Id = id;
        return fee;
    }

    public async Task<bool> ExistsByTransferIdAsync(string transferId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Fees
            WHERE TransferId = @TransferId";

        var count = await _connection.QuerySingleAsync<int>(
            new CommandDefinition(sql, new { TransferId = transferId }, cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<IEnumerable<Fee>> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, TransferId, AccountNumber, TransferAmount, FeeAmount, AppliedAt
            FROM Fees
            WHERE AccountNumber = @AccountNumber
            ORDER BY AppliedAt DESC
            LIMIT 100";

        var results = await _connection.QueryAsync<FeeDto>(
            new CommandDefinition(sql, new { AccountNumber = accountNumber }, cancellationToken: cancellationToken));

        return results.Select(dto => dto.ToDomain());
    }

    public async Task<IEnumerable<Fee>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, TransferId, AccountNumber, TransferAmount, FeeAmount, AppliedAt
            FROM Fees
            ORDER BY AppliedAt DESC
            LIMIT @Take OFFSET @Skip";

        var results = await _connection.QueryAsync<FeeDto>(
            new CommandDefinition(sql, new { Skip = skip, Take = take }, cancellationToken: cancellationToken));

        return results.Select(dto => dto.ToDomain());
    }
}

internal class FeeDto
{
    public int Id { get; set; }
    public string TransferId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal TransferAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public DateTime AppliedAt { get; set; }

    public Fee ToDomain()
    {
        var fee = Fee.Create(TransferId, AccountNumber, TransferAmount, FeeAmount);
        fee.Id = Id;
        fee.AppliedAt = AppliedAt;
        return fee;
    }

    public static FeeDto FromDomain(Fee fee)
    {
        return new FeeDto
        {
            TransferId = fee.TransferId,
            AccountNumber = fee.AccountNumber,
            TransferAmount = fee.TransferAmount,
            FeeAmount = fee.FeeAmount,
            AppliedAt = fee.AppliedAt
        };
    }
}

