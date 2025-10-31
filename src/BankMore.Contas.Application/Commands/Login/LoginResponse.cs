namespace BankMore.Contas.Application.Commands.Login;

/// <summary>
/// Resposta do login com token JWT
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Token JWT para autenticação nas requisições seguintes
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// ID interno da conta
    /// </summary>
    /// <example>1</example>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta corrente
    /// </summary>
    /// <example>1234567890</example>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Data e hora de expiração do token (UTC)
    /// </summary>
    /// <example>2024-12-31T23:59:59Z</example>
    public DateTime ExpiresAt { get; set; }
}

