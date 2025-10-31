namespace BankMore.Contas.Application.Services;

public class TokenResult
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public interface ITokenService
{
    TokenResult GenerateToken(int accountId, string accountNumber);
    bool ValidateToken(string token, out int accountId, out string accountNumber);
}

