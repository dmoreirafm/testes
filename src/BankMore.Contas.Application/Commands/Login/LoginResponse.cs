namespace BankMore.Contas.Application.Commands.Login;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

