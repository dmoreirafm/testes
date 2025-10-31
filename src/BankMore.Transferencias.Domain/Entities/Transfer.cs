using BankMore.Transferencias.Domain.Enums;

namespace BankMore.Transferencias.Domain.Entities;

public class Transfer
{
    public int Id { get; set; }
    public string RequestId { get; private set; }
    public string OriginAccountNumber { get; private set; }
    public string DestinationAccountNumber { get; private set; }
    public decimal Amount { get; private set; }
    public TransferStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    private Transfer() { } // Para Dapper

    private Transfer(string requestId, string originAccountNumber, string destinationAccountNumber, decimal amount)
    {
        RequestId = requestId;
        OriginAccountNumber = originAccountNumber;
        DestinationAccountNumber = destinationAccountNumber;
        Amount = amount;
        Status = TransferStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public static Transfer Create(string requestId, string originAccountNumber, string destinationAccountNumber, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new Common.DomainException("RequestId não pode ser vazio.", "INVALID_REQUEST_ID");

        if (string.IsNullOrWhiteSpace(originAccountNumber))
            throw new Common.DomainException("Número da conta de origem não pode ser vazio.", "INVALID_ACCOUNT");

        if (string.IsNullOrWhiteSpace(destinationAccountNumber))
            throw new Common.DomainException("Número da conta de destino não pode ser vazio.", "INVALID_ACCOUNT");

        if (originAccountNumber == destinationAccountNumber)
            throw new Common.DomainException("Conta de origem e destino não podem ser iguais.", "INVALID_OPERATION");

        if (amount <= 0)
            throw new Common.DomainException("Valor deve ser positivo.", "INVALID_VALUE");

        return new Transfer(requestId, originAccountNumber, destinationAccountNumber, amount);
    }

    public void MarkAsCompleted()
    {
        if (Status != TransferStatus.Pending)
            throw new Common.DomainException("Apenas transferências pendentes podem ser concluídas.", "INVALID_STATUS");

        Status = TransferStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason)
    {
        Status = TransferStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompensated()
    {
        Status = TransferStatus.Compensated;
        UpdatedAt = DateTime.UtcNow;
    }
}

