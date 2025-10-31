namespace BankMore.Transferencias.Application.Services;

public class MakeTransactionRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public decimal Amount { get; set; }
    public char Type { get; set; }
}

public class MakeTransactionResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal NewBalance { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IAccountsApiClient
{
    Task<MakeTransactionResponse> MakeDebitAsync(string requestId, string accountNumber, decimal amount, string jwtToken, CancellationToken cancellationToken = default);
    Task<MakeTransactionResponse> MakeCreditAsync(string requestId, string accountNumber, decimal amount, string jwtToken, CancellationToken cancellationToken = default);
    Task<GetBalanceResponse> GetBalanceAsync(string accountNumber, string jwtToken, CancellationToken cancellationToken = default);
}

public class GetBalanceResponse
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime ConsultedAt { get; set; }
}

