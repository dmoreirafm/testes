using BankMore.Transferencias.Domain.Entities;
using BankMore.Transferencias.Domain.Enums;
using Dapper;
using System.Data;

namespace BankMore.Transferencias.Infrastructure.Repositories;

public class TransferRepository : Domain.Repositories.ITransferRepository
{
    private readonly IDbConnection _connection;

    public TransferRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Transfer?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, RequestId, OriginAccountNumber, DestinationAccountNumber, Amount, Status, FailureReason, CreatedAt, UpdatedAt
            FROM Transfers
            WHERE RequestId = @RequestId";

        var result = await _connection.QueryFirstOrDefaultAsync<TransferDto>(
            new CommandDefinition(sql, new { RequestId = requestId }, cancellationToken: cancellationToken));

        return result?.ToDomain();
    }

    public async Task<Transfer> CreateAsync(Transfer transfer, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Transfers (RequestId, OriginAccountNumber, DestinationAccountNumber, Amount, Status, CreatedAt)
            VALUES (@RequestId, @OriginAccountNumber, @DestinationAccountNumber, @Amount, @Status, @CreatedAt);
            SELECT last_insert_rowid();";

        var transferDto = TransferDto.FromDomain(transfer);
        var id = await _connection.QuerySingleAsync<int>(
            new CommandDefinition(sql, transferDto, cancellationToken: cancellationToken));

        transfer.Id = id;
        return transfer;
    }

    public async Task UpdateAsync(Transfer transfer, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Transfers
            SET Status = @Status, FailureReason = @FailureReason, UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                Id = transfer.Id,
                Status = (int)transfer.Status,
                FailureReason = transfer.FailureReason,
                UpdatedAt = transfer.UpdatedAt
            }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsByRequestIdAsync(string requestId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Transfers
            WHERE RequestId = @RequestId";

        var count = await _connection.QuerySingleAsync<int>(
            new CommandDefinition(sql, new { RequestId = requestId }, cancellationToken: cancellationToken));

        return count > 0;
    }
}

internal class TransferDto
{
    public int Id { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string OriginAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Transfer ToDomain()
    {
        var transfer = Transfer.Create(RequestId, OriginAccountNumber, DestinationAccountNumber, Amount);
        transfer.Id = Id;
        transfer.Status = (TransferStatus)Status;
        transfer.FailureReason = FailureReason;
        transfer.CreatedAt = CreatedAt;
        transfer.UpdatedAt = UpdatedAt;
        return transfer;
    }

    public static TransferDto FromDomain(Transfer transfer)
    {
        return new TransferDto
        {
            RequestId = transfer.RequestId,
            OriginAccountNumber = transfer.OriginAccountNumber,
            DestinationAccountNumber = transfer.DestinationAccountNumber,
            Amount = transfer.Amount,
            Status = (int)transfer.Status,
            FailureReason = transfer.FailureReason,
            CreatedAt = transfer.CreatedAt,
            UpdatedAt = transfer.UpdatedAt
        };
    }
}

