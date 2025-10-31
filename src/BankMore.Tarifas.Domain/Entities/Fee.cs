using BankMore.Tarifas.Domain.Common;

namespace BankMore.Tarifas.Domain.Entities;

public class Fee
{
    public int Id { get; set; }
    public string TransferId { get; private set; }
    public string AccountNumber { get; private set; }
    public decimal TransferAmount { get; private set; }
    public decimal FeeAmount { get; private set; }
    public DateTime AppliedAt { get; set; }

    private Fee() { } // Para Dapper

    private Fee(string transferId, string accountNumber, decimal transferAmount, decimal feeAmount)
    {
        TransferId = transferId;
        AccountNumber = accountNumber;
        TransferAmount = transferAmount;
        FeeAmount = feeAmount;
        AppliedAt = DateTime.UtcNow;
    }

    public static Fee Create(string transferId, string accountNumber, decimal transferAmount, decimal feeAmount)
    {
        if (string.IsNullOrWhiteSpace(transferId))
            throw new DomainException("TransferId não pode ser vazio.", "INVALID_TRANSFER_ID");

        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new DomainException("Número da conta não pode ser vazio.", "INVALID_ACCOUNT_NUMBER");

        if (transferAmount <= 0)
            throw new DomainException("Valor da transferência deve ser positivo.", "INVALID_TRANSFER_AMOUNT");

        if (feeAmount <= 0)
            throw new DomainException("Valor da tarifa deve ser positivo.", "INVALID_FEE_AMOUNT");

        return new Fee(transferId, accountNumber, transferAmount, feeAmount);
    }
}

