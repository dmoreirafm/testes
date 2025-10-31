using BankMore.Contas.Domain.Enums;

namespace BankMore.Contas.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; private set; }
    public string RequestId { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public DateTime CreatedAt { get; set; }

    private Transaction() { } // Para Dapper

    private Transaction(int accountId, string requestId, decimal amount, TransactionType type)
    {
        AccountId = accountId;
        RequestId = requestId;
        Amount = amount;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }

    public static Transaction Create(int accountId, string requestId, decimal amount, TransactionType type)
    {
        if (accountId <= 0)
            throw new Common.DomainException("ID da conta inválido.", "INVALID_ACCOUNT");

        if (string.IsNullOrWhiteSpace(requestId))
            throw new Common.DomainException("RequestId não pode ser vazio.", "INVALID_REQUEST_ID");

        if (amount <= 0)
            throw new Common.DomainException("Valor deve ser positivo.", "INVALID_VALUE");

        return new Transaction(accountId, requestId, amount, type);
    }

    public bool IsCredit() => Type == TransactionType.Credit;
    public bool IsDebit() => Type == TransactionType.Debit;
}

