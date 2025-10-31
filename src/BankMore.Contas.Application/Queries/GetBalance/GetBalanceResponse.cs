namespace BankMore.Contas.Application.Queries.GetBalance;

public class GetBalanceResponse
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime ConsultedAt { get; set; }
}

