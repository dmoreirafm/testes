namespace BankMore.Tarifas.Domain.Messages;

public class TransferRealizedMessage
{
    public string RequestId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal TransferAmount { get; set; }
}

