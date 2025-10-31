namespace BankMore.Transferencias.Application.Commands.CreateTransfer;

public class CreateTransferResponse
{
    public string TransferId { get; set; } = string.Empty;
    public string OriginAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

