namespace BankMore.Contas.Application.Commands.MakeTransaction;

public class MakeTransactionResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal NewBalance { get; set; }
    public DateTime CreatedAt { get; set; }
}

