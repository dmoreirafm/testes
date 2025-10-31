namespace BankMore.Contas.Domain.Messages;

public class FeeAppliedMessage
{
    public string AccountNumber { get; set; } = string.Empty;
    public decimal FeeAmount { get; set; }
}

